using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Exceptions;
using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Resources;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Queries;
using ConversationalSearchPlatform.BackOffice.Services.OpenAIHelpers;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using GraphQL;
using HtmlAgilityPack;
using Jint;
using Jint.Fetch;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using OpenAI;
using OpenAI.Chat;
using Language = ConversationalSearchPlatform.BackOffice.Services.Models.Language;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public partial class ConversationService : IConversationService
{
    private readonly IMemoryCache _conversationsCache;
    private readonly IMultiTenantStore<ApplicationTenantInfo> _tenantStore;
    private readonly IVectorizationService _vectorizationService;
    private readonly IOpenAIUsageTelemetryService _telemetryService;
    private readonly IKeywordExtractorService _keywordExtractorService;
    private readonly IRagService _ragService;
    private readonly ILogger<ConversationService> _logger;
    private readonly OpenAIClient _openAIClient;


    private readonly MemoryCacheEntryOptions _defaultMemoryCacheEntryOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(2)
    };


    private readonly ChatTool searchTool = ChatTool.CreateFunctionTool(
        functionName: nameof(searchTool),
        functionDescription: "Search the knowledge base. Results are formatted as a XML-document containing a list of possible Sources that have a list of References as a property. Use the ReferenceId to identify a reference.",
        functionParameters: BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {
                    "query": {
                        "type": "string",
                        "description": "Search query"
                    }
                },
                "required": ["query"],
                "additionalProperties": false
            }
        """u8.ToArray()));

    private readonly ChatTool groundingTool = ChatTool.CreateFunctionTool(
        functionName: nameof(groundingTool),
        functionDescription: "Report use of a source from the knowledge base as part of an answer (effectively, cite the source). Sources appear as references in the XML-document. Use the ReferenceId property only to identify a reference. Always use this tool to cite sources when responding with information from the knowledge base.",
        functionParameters: BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {
                    "sources": {
                        "type": "array",
                        "items": {
                            "type": "number"
                        },
                        "description": "List of ReferenceIds of the sources from last statement actually used, do not include the ones not used to formulate a response"
                    }
                },
                "required": ["sources"],
                "additionalProperties": false
            }
        """u8.ToArray()));

    public ConversationService(
        IMemoryCache conversationsCache,
        OpenAIClient openAIClient,
        IMultiTenantStore<ApplicationTenantInfo> tenantStore,
        IVectorizationService vectorizationService,
        ILogger<ConversationService> logger,
        IOpenAIUsageTelemetryService telemetryService,
        IKeywordExtractorService keywordExtractorService,
        IRagService ragService)
    {
        _conversationsCache = conversationsCache;
        _openAIClient = openAIClient;
        _tenantStore = tenantStore;
        _vectorizationService = vectorizationService;
        _logger = logger;
        _telemetryService = telemetryService;
        _keywordExtractorService = keywordExtractorService;
        _ragService = ragService;
    }

    public Task<ConversationId> StartConversationAsync(StartConversation startConversation, CancellationToken cancellationToken)
    {
        var conversationId = Guid.NewGuid();
        var cacheKey = GetCacheKey(conversationId);
        _conversationsCache.Set(cacheKey,
            new ConversationHistory(startConversation.Model, startConversation.AmountOfSearchReferences),
            _defaultMemoryCacheEntryOptions);

        return Task.FromResult(new ConversationId(conversationId));
    }

    public async Task<ConversationReferencedResult> ConverseAsync(HoldConversation holdConversation, CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(holdConversation.ConversationId);
        var conversationHistory = GetConversationHistory(holdConversation, cacheKey);
        conversationHistory.DebugEnabled = holdConversation.Debug;
        conversationHistory.IsStreaming = false;

        List<SortedSearchReference> indexedTextReferences = new List<SortedSearchReference>();
        var validReferences = new List<ConversationReference>();

        var toolImageReferences = new List<ImageSearchReference>();

        bool shouldEndConversation = false;

        // are we at the maximum number of follow up questions after this one?
        if (conversationHistory.AnsweredMessages >= 5)
        {
            shouldEndConversation = true;
        }

        bool requiresAction = false;
        var chatClient = _openAIClient.GetChatClient("gpt4");

        if (conversationHistory.Messages.Count == 0)
        {
            var tenantId = holdConversation.TenantId;
            var tenant = await GetTenantAsync(tenantId);
            var promptBuilder = new PromptBuilder(new StringBuilder((await ResourceHelper.GetEmbeddedResourceTextAsync(ResourceHelper.BasePromptFile)).Trim()))
                .ReplaceTenantPrompt(GetTenantPrompt(tenant))
                .ReplaceConversationContextVariables(tenant.PromptTags ?? new List<PromptTag>(), holdConversation.ConversationContext);

            conversationHistory.AppendToConversation(new SystemChatMessage(promptBuilder.Build()));
        }

        conversationHistory.AppendToConversation(holdConversation.UserPrompt, false, true);
        conversationHistory.AppendToConversation(new UserChatMessage("Do not give me any information that is not mentioned in the <SOURCES> document or in the tenant prompt <TenantPrompt>. Only use the functions you have been provided with."), true);

        string assistantAnswer = string.Empty;

        do
        {
            requiresAction = false;
            ChatCompletion chatCompletion = await chatClient.CompleteChatAsync(conversationHistory.Messages, new ChatCompletionOptions()
            {
                Temperature = 0.7f,
                Tools = { searchTool, groundingTool },
            });

            switch (chatCompletion.FinishReason)
            {
                case ChatFinishReason.Stop:
                    {
                        // Add the assistant message to the conversation history.
                        conversationHistory.AppendToConversation(new AssistantChatMessage(chatCompletion));

                        _telemetryService.RegisterGPTUsage(
                            holdConversation.ConversationId,
                            holdConversation.TenantId,
                            chatCompletion.Usage ?? throw new InvalidOperationException("No usage was passed in after executing an OpenAI call"),
                            conversationHistory.Model
                        );
                        conversationHistory.DebugInformation?.SetUsage(chatCompletion.Usage.InputTokenCount, chatCompletion.Usage.OutputTokenCount);

                        Console.WriteLine($"Assistant: {chatCompletion.Content[0].Text}");
                        assistantAnswer = chatCompletion.Content[0].Text;
                        break;
                    }

                case ChatFinishReason.ToolCalls:
                    {
                        // First, add the assistant message with tool calls to the conversation history.
                        conversationHistory.AppendToConversation(new AssistantChatMessage(chatCompletion));

                        // Then, add a new tool message for each tool call that is resolved.
                        foreach (ChatToolCall toolCall in chatCompletion.ToolCalls)
                        {
                            switch (toolCall.FunctionName)
                            {
                                case nameof(searchTool):
                                    {
                                        // The arguments that the model wants to use to call the function are specified as a
                                        // stringified JSON object based on the schema defined in the tool definition. Note that
                                        // the model may hallucinate arguments too. Consequently, it is important to do the
                                        // appropriate parsing and validation before calling the function.
                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                        bool hasQuery = argumentsJson.RootElement.TryGetProperty("query", out JsonElement query);

                                        if (!hasQuery)
                                        {
                                            throw new ArgumentNullException(nameof(query), "The query argument is required.");
                                        }
                                        Console.WriteLine($"Query: {query}");

                                        var vector = await _vectorizationService.CreateVectorAsync(holdConversation.ConversationId, holdConversation.TenantId, UsageType.Conversation, query.GetString());

                                        var ragDocument = await _ragService.GetRAGDocumentAsync(Guid.Parse(holdConversation.TenantId));

                                        List<TextSearchReference> references = new List<TextSearchReference>();
                                        var index = conversationHistory.LastReferenceIndex;

                                        foreach (var ragClass in ragDocument.Classes)
                                        {
                                            var toolTextReferences = await GetTextReferences(
                                                conversationHistory,
                                                nameof(WebsitePage),
                                                holdConversation.TenantId,
                                                "English",
                                                ragClass.Name,
                                                query.GetString(),
                                                vector,
                                                cancellationToken);

                                            foreach (var reference in toolTextReferences)
                                            {
                                                index++;

                                                Dictionary<string, string> properties = new Dictionary<string, string>();
                                                properties["Content"] = reference.Content;
                                                properties["Title"] = reference.Title;

                                                ragClass.Sources.Add(new RAGSource()
                                                {
                                                    ReferenceId = index,
                                                    Properties = properties,
                                                });

                                                indexedTextReferences.Add(new SortedSearchReference()
                                                {
                                                    Index = index,
                                                    TextSearchReference = reference,
                                                });
                                            }

                                            references.AddRange(toolTextReferences);
                                        }


                                        var ragString = await ragDocument.GenerateXMLStringAsync();

                                        conversationHistory.AppendToConversation(new ToolChatMessage(toolCall.Id, ragString));
                                        break;
                                    }
                                case nameof(groundingTool):
                                    {
                                        // The arguments that the model wants to use to call the function are specified as a
                                        // stringified JSON object based on the schema defined in the tool definition. Note that
                                        // the model may hallucinate arguments too. Consequently, it is important to do the
                                        // appropriate parsing and validation before calling the function.
                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                        bool hasSources = argumentsJson.RootElement.TryGetProperty("sources", out JsonElement sources);

                                        if (!hasSources)
                                        {
                                            throw new ArgumentNullException(nameof(sources), "The sources argument is required.");
                                        }

                                        validReferences.AddRange(sources.EnumerateArray()
                                            .ToList()
                                            .Select(source =>
                                            {
                                                var index = source.GetInt32();
                                                var reference = indexedTextReferences.First(x => x.Index == index);

                                                return new ConversationReference(index, 
                                                    reference.TextSearchReference.Source,
                                                    reference.TextSearchReference.Type,
                                                    reference.TextSearchReference.Title);
                                            }));

                                        conversationHistory.AppendToConversation(new ToolChatMessage(toolCall.Id, "results_received, continue"));
                                        break;
                                    }

                                default:
                                    {
                                        // Handle other unexpected calls.
                                        throw new NotImplementedException();
                                    }
                            }
                        }

                        requiresAction = true;
                        break;
                    }

                case ChatFinishReason.Length:
                    throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");

                case ChatFinishReason.ContentFilter:
                    throw new NotImplementedException("Omitted content due to a content filter flag.");

                case ChatFinishReason.FunctionCall:
                    throw new NotImplementedException("Deprecated in favor of tool calls.");

                default:
                    throw new NotImplementedException(chatCompletion.FinishReason.ToString());
            }
        } while (requiresAction);

        conversationHistory.HasEnded = shouldEndConversation;
        conversationHistory.SaveConversationHistory(_conversationsCache, cacheKey);

        //var conversationReferencedResult = ParseAnswerWithReferences(holdConversation, conversationHistory, indexedTextReferences, toolImageReferences, shouldEndConversation);

        return new ConversationReferencedResult(
            new ConversationResult(
                holdConversation.ConversationId,
                assistantAnswer,
                holdConversation.Language
            ),
            validReferences,
            shouldEndConversation,
            conversationHistory.DebugInformation
        );
    }

    public async IAsyncEnumerable<ConversationReferencedResult> ConverseStreamingAsync(
        HoldConversation holdConversation,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(holdConversation.ConversationId);
        var conversationHistory = GetConversationHistory(holdConversation, cacheKey);
        conversationHistory.DebugEnabled = holdConversation.Debug;
        conversationHistory.IsStreaming = false;

        List<SortedSearchReference> indexedTextReferences = new List<SortedSearchReference>();
        var toolImageReferences = new List<ImageSearchReference>();
        var validReferences = new List<ConversationReference>();

        bool shouldEndConversation = false;

        // are we at the maximum number of follow up questions after this one?
        if (conversationHistory.AnsweredMessages >= 5)
        {
            shouldEndConversation = true;
        }

        bool requiresAction = false;
        var chatClient = _openAIClient.GetChatClient("gpt4");

        if (conversationHistory.Messages.Count == 0)
        {
            var tenantId = holdConversation.TenantId;
            var tenant = await GetTenantAsync(tenantId);
            var promptBuilder = new PromptBuilder(new StringBuilder((await ResourceHelper.GetEmbeddedResourceTextAsync(ResourceHelper.BasePromptFile)).Trim()))
                .ReplaceTenantPrompt(GetTenantPrompt(tenant))
                .ReplaceConversationContextVariables(tenant.PromptTags ?? new List<PromptTag>(), holdConversation.ConversationContext);

            conversationHistory.AppendToConversation(new SystemChatMessage(promptBuilder.Build()));
        }

        conversationHistory.AppendToConversation(holdConversation.UserPrompt, false, true);
        conversationHistory.AppendToConversation(new UserChatMessage("Do not give me any information that is not mentioned in the <SOURCES> document or in the tenant prompt <TenantPrompt>. Only use the functions you have been provided with."), true);

        StreamingChatToolCallsBuilder toolCallsBuilder = new();
        StringBuilder contentBuilder = new();

        do
        {
            requiresAction = false;
            var completionUpdates = chatClient.CompleteChatStreamingAsync(conversationHistory.Messages, new ChatCompletionOptions()
            {
                Temperature = 0.7f,
                Tools = { searchTool, groundingTool },
            }, cancellationToken);


            await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
            {
                completionUpdate.ToolCallUpdates.ToList().ForEach(toolCallsBuilder.Append);

                foreach (var contentUpdate in completionUpdate.ContentUpdate)
                {
                    if (contentUpdate != null)
                    {
                        contentBuilder.Append(contentUpdate.Text);

                        yield return new ConversationReferencedResult(
                            new ConversationResult(holdConversation.ConversationId, contentUpdate.Text, holdConversation.Language),
                            new(), shouldEndConversation, null);
                    }
                }

                if (completionUpdate.FinishReason != null)
                {
                    switch (completionUpdate.FinishReason)
                    {
                        case ChatFinishReason.Stop:
                            {
                                // Add the assistant message to the conversation history.
                                var message = contentBuilder.ToString();
                                Console.WriteLine(message);
                                conversationHistory.AppendToConversation(new AssistantChatMessage(message));

                                /*_telemetryService.RegisterGPTUsage(
                                    holdConversation.ConversationId,
                                    holdConversation.TenantId,
                                    completionUpdate.Usage ?? throw new InvalidOperationException("No usage was passed in after executing an OpenAI call"),
                                    conversationHistory.Model
                                );
                                conversationHistory.DebugInformation?.SetUsage(completionUpdate.Usage.InputTokenCount, 
                                    completionUpdate.Usage.OutputTokenCount);*/

                                // send references
                                yield return new ConversationReferencedResult(
                                    new ConversationResult(holdConversation.ConversationId, string.Empty, holdConversation.Language),
                                    validReferences, shouldEndConversation, null);

                                break;
                            }

                        case ChatFinishReason.ToolCalls:
                            {
                                var toolCalls = toolCallsBuilder.Build();
                                AssistantChatMessage assistantMessage = new AssistantChatMessage(toolCalls);
                                conversationHistory.AppendToConversation(assistantMessage);

                                // Then, add a new tool message for each tool call that is resolved.
                                foreach (ChatToolCall toolCall in toolCalls)
                                {
                                    // First, add the assistant message with tool calls to the conversation history.

                                    switch (toolCall.FunctionName)
                                    {
                                        case nameof(searchTool):
                                            {
                                                // The arguments that the model wants to use to call the function are specified as a
                                                // stringified JSON object based on the schema defined in the tool definition. Note that
                                                // the model may hallucinate arguments too. Consequently, it is important to do the
                                                // appropriate parsing and validation before calling the function.
                                                using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                                bool hasQuery = argumentsJson.RootElement.TryGetProperty("query", out JsonElement query);

                                                if (!hasQuery)
                                                {
                                                    throw new ArgumentNullException(nameof(query), "The query argument is required.");
                                                }
                                                Console.WriteLine($"Query: {query}");

                                                var vector = await _vectorizationService.CreateVectorAsync(holdConversation.ConversationId, holdConversation.TenantId, UsageType.Conversation, query.GetString());

                                                var ragDocument = await _ragService.GetRAGDocumentAsync(Guid.Parse(holdConversation.TenantId));

                                                List<TextSearchReference> references = new List<TextSearchReference>();
                                                var index = conversationHistory.LastReferenceIndex;

                                                foreach (var ragClass in ragDocument.Classes)
                                                {
                                                    var toolTextReferences = await GetTextReferences(
                                                        conversationHistory,
                                                        nameof(WebsitePage),
                                                        holdConversation.TenantId,
                                                        "English",
                                                        ragClass.Name,
                                                        query.GetString(),
                                                        vector,
                                                        cancellationToken);

                                                    foreach (var reference in toolTextReferences)
                                                    {
                                                        index++;

                                                        Dictionary<string, string> properties = new Dictionary<string, string>();
                                                        properties["Content"] = reference.Content;
                                                        properties["Title"] = reference.Title;

                                                        ragClass.Sources.Add(new RAGSource()
                                                        {
                                                            ReferenceId = index,
                                                            Properties = properties,
                                                        });

                                                        indexedTextReferences.Add(new SortedSearchReference()
                                                        {
                                                            Index = index,
                                                            TextSearchReference = reference,
                                                        });
                                                    }

                                                    references.AddRange(toolTextReferences);
                                                }


                                                var ragString = await ragDocument.GenerateXMLStringAsync();
                                                conversationHistory.LastReferenceIndex = index;

                                                conversationHistory.AppendToConversation(new ToolChatMessage(toolCall.Id, ragString));
                                                break;
                                            }
                                        case nameof(groundingTool):
                                            {
                                                // The arguments that the model wants to use to call the function are specified as a
                                                // stringified JSON object based on the schema defined in the tool definition. Note that
                                                // the model may hallucinate arguments too. Consequently, it is important to do the
                                                // appropriate parsing and validation before calling the function.
                                                using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                                bool hasSources = argumentsJson.RootElement.TryGetProperty("sources", out JsonElement sources);

                                                if (!hasSources)
                                                {
                                                    throw new ArgumentNullException(nameof(sources), "The sources argument is required.");
                                                }

                                                validReferences.AddRange(sources.EnumerateArray()
                                                    .ToList()
                                                    .Select(source =>
                                                    {
                                                        var index = source.GetInt32();
                                                        var reference = indexedTextReferences.First(x => x.Index == index);

                                                        return new ConversationReference(index,
                                                            reference.TextSearchReference.Source,
                                                            reference.TextSearchReference.Type,
                                                            reference.TextSearchReference.Title);
                                                    }));

                                                conversationHistory.AppendToConversation(new ToolChatMessage(toolCall.Id, "results_received, continue"));
                                                break;
                                            }

                                        default:
                                            {
                                                // Handle other unexpected calls.
                                                throw new NotImplementedException();
                                            }
                                    }
                                }

                                requiresAction = true;
                                break;
                            }

                        case ChatFinishReason.Length:
                            throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");

                        case ChatFinishReason.ContentFilter:
                            throw new NotImplementedException("Omitted content due to a content filter flag.");

                        case ChatFinishReason.FunctionCall:
                            throw new NotImplementedException("Deprecated in favor of tool calls.");

                        default:
                            throw new NotImplementedException(completionUpdate.FinishReason.ToString());
                    }

                    toolCallsBuilder.Clear();
                    contentBuilder.Clear();
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        } while (requiresAction);

        conversationHistory.HasEnded = shouldEndConversation;
        conversationHistory.SaveConversationHistory(_conversationsCache, cacheKey);
    }

    public async Task<ConversationContext> GetConversationContext(GetConversationContext getConversationContext)
    {
        var tenant = await _tenantStore.TryGetAsync(getConversationContext.TenantId);

        if (tenant == null)
        {
            return new ConversationContext(Enumerable.Empty<string>().ToList());
        }

        var tags = tenant.PromptTags?.Select(tag => tag.Value.TrimStart('{').TrimEnd('}')) ?? Enumerable.Empty<string>();
        return new ConversationContext(tags.ToList());
    }

    private (string functionResults, string keywordString) CallFunction(string? functionName, string? arguments)
    {
        string keywordString = string.Empty;
        dynamic context = JObject.Parse(arguments);

        var engine = new Engine();
        engine.SetValue("log", new Action<object>((obj) => _logger.LogInformation(obj.ToString())))
            .SetValue("parseHtml", new Func<string, HtmlAgilityPack.HtmlDocument>((html) =>
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                return htmlDoc;
            }))
            .SetValue("fetch", new Func<string, object, Task<FetchResult>>((uri, options) => FetchClass.Fetch(uri, FetchClass.ExpandoToOptionsObject(options))))
            .SetValue("__keyword_string", keywordString)
            .SetValue("__result", 0)
            .SetValue("ctx", context);

        try
        {
            engine.Execute(
                """
(async () => {
    async function GetRecommendedProducts(gender, mobility, incontinence_level) { 
        log(`Call product selector with: ${gender}, ${mobility}, ${incontinence_level}`);
        // create body
        var genderData;
        if (gender == "male") {
            genderData = {"data-title":"men_inco,women_men_inco", "data-details":"Men", "data-description":"men"}
        } else {
            genderData = {"data-title":"women_inco,women_men_inco", "data-details":"Women", "data-description":"women"}
        }

        var mobilityData;
        switch(mobility) {
            case "mobile":
                mobilityData = {"data-title":"FullMobility", "data-details":"fullMobilityScore", "data-description":"Able"};
                break;
            case "needs_help_toilet":
                mobilityData = {"data-title":"NeedAssistance", "data-details":"needAssistanceScore", "data-description":"Needs"};
                break;
            case "bedridden":
                mobilityData = {"data-title":"Bedridden", "data-details":"bedriddenScore", "data-description":"Unable"};
                break;
        }

        var absorptionData;
        switch(incontinence_level) {
            case "small":
                absorptionData = {"data-title":"0_5-drop-inco,1-drop-inco","data-description":"VeryLight","data-details":"0_5-drop-inco,1-drop-inco","data-nearestrange":"1_5-drop-inco,2_5-drop-inco,2-drop-inco"};
                break;
            case "light":
                absorptionData = {"data-title":"1_5-drop-inco,2-drop-inco,2_5-drop-inco","data-description":"Light","data-details":"1_5-drop-inco,2_5-drop-inco,2-drop-inco","data-nearestrange":"3_5-drop-inco,3-drop-inco,4_5-drop-inco,4-drop-inco,5-drop-inco"};
                break;
            case "moderate":
                absorptionData = {"data-title":"3-drop-inco,3_5-drop-inco,4-drop-inco,4_5-drop-inco,5-drop-inco","data-description":"Medium","data-details":"3_5-drop-inco,3-drop-inco,4_5-drop-inco,4-drop-inco,5-drop-inco","data-nearestrange":"5_5-drop-inco,6_5-drop-inco,6-drop-inco,7-drop-inco"};
                break;
            case "heavy":
                absorptionData = {"data-title":"5_5-drop-inco,6-drop-inco,6_5-drop-inco,7-drop-inco", "data-details":"5_5-drop-inco,6_5-drop-inco,6-drop-inco,7-drop-inco", "data-description":"Heavy", "data-nearestrange":"7_5-drop-inco,8_5-drop-inco,8-drop-inco,9-drop-inco"};
                break;
            case "very_heavy":
                absorptionData = {"data-title":"7_5-drop-inco,8-drop-inco,8_5-drop-inco,9-drop-inco", "data-details":"7_5-drop-inco,8_5-drop-inco,8-drop-inco,9-drop-inco", "data-description":"VeryHeavy", "data-nearestrange":"5_5-drop-inco,6_5-drop-inco,6-drop-inco,7-drop-inco"};
                break;
        }

        var body = JSON.stringify({
            User: {"data-title":"Homecare", "data-details":"Pharmacist", "data-description":"Pharmacist"},
            Gender: genderData,
            Mobility: mobilityData,
            Absorption: absorptionData
        });

        log('Call productor selector api with body: ' + body);

        var response = await fetch("https://www.tena.co.uk/professionals/api/Services/ProductFinder/GetProductSelectorResult", { Method: "POST", Body: { inputData: body } });
        var content = await response.json();

        return { 
            recommended: content.recommendedProduct.recommendedProduct.productName,
            additional: [...content.considerationProducts.products].map((p) => p.productName),
        };
    };

    __result = await GetRecommendedProducts(ctx.gender, ctx.mobility, ctx.incontinence_level);
})();
""");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }


        string jsonString = JsonSerializer.Serialize(engine.GetValue("__result").ToObject());

        _logger.LogInformation($"Result of function call: {jsonString}");

        return (jsonString, keywordString);
    }

    private async Task<ApplicationTenantInfo> GetTenantAsync(string tenantId)
    {
        var tenant = await _tenantStore.TryGetAsync(tenantId);

        if (tenant == null)
        {
            ThrowHelper.ThrowTenantNotFoundException(tenantId);
        }

        return tenant;
    }

    /*private static ConversationReferencedResult ParseAnswerWithReferences(HoldConversation holdConversation,
        ConversationHistory conversationHistory,
        IReadOnlyCollection<SortedSearchReference> textReferences,
        List<ImageSearchReference> imageReferences,
        bool endConversation)
    {
        string mergedAnswer = string.Empty;

        var isStreaming = conversationHistory.IsStreaming;
        var isLastChunk = conversationHistory.IsLastChunk;
        var shouldReturnFullMessage = false;

        if (isStreaming && isLastChunk)
        {
            shouldReturnFullMessage = true;
            mergedAnswer = conversationHistory.GetAllStreamingResponseChunksMerged();
        }
        else if (!isStreaming)
        {
            shouldReturnFullMessage = true;
            mergedAnswer = conversationHistory.Messages.Last().Content.FirstOrDefault()?.Text ?? string.Empty;
        }

        Console.WriteLine($"Merged answer {mergedAnswer}");

        List<ConversationReference>? validReferences = null;
        if (shouldReturnFullMessage)
        {
            validReferences = DetermineValidReferences(textReferences, mergedAnswer);

            if (conversationHistory.DebugEnabled)
            {
                conversationHistory.AppendPostRequestTextReferenceDebugInformation(validReferences);
                conversationHistory.AppendPostRequestImageReferenceDebugInformation(imageReferences, mergedAnswer);
            }
        }

        return new ConversationReferencedResult(
            new ConversationResult(
                holdConversation.ConversationId,
                isStreaming ? conversationHistory.StreamingResponseChunks.Last() : mergedAnswer,
                holdConversation.Language
            ),
            validReferences ?? new(),
            endConversation,
            conversationHistory.DebugInformation
        );
    }*/


    /*private static List<ConversationReference> DetermineValidReferences(
        IReadOnlyCollection<SortedSearchReference> textReferences,
        string mergedAnswer)
    {
        var sourceRegex = SourceIndexRegex();
        var matches = sourceRegex.Matches(mergedAnswer);

        var validReferences = new List<ConversationReference>();

        foreach (Match match in matches)
        {
            var idx = match.Value
                .TrimStart('[')
                .TrimEnd(']');

            var parsed = int.TryParse(idx, out var parsedIdx);

            if (parsed)
            {
                var sortedSearchReference = textReferences.FirstOrDefault(reference => reference.Index == parsedIdx);

                if (sortedSearchReference != null)
                {
                    var reference = new ConversationReference(
                        parsedIdx,
                        sortedSearchReference.TextSearchReference.Source,
                        sortedSearchReference.TextSearchReference.Type,
                        sortedSearchReference.TextSearchReference.Title
                    );

                    if (validReferences.All(conversationReference => conversationReference.Index != parsedIdx))
                    {
                        validReferences.Add(reference);
                    }
                }
            }
        }

        return validReferences
            .OrderBy(reference => reference.Index)
            .ToList();
    }*/

    private ConversationHistory GetConversationHistory(HoldConversation holdConversation, string cacheKey)
    {
        if (!_conversationsCache.TryGetValue(cacheKey, out ConversationHistory? conversationHistory))
        {
            ThrowHelper.ThrowConversationNotFoundException(holdConversation.ConversationId);
        }

        return conversationHistory!;
    }


    private static string GetTenantPrompt(ApplicationTenantInfo tenant) => tenant.GetBasePromptOrDefault();

    private async Task<List<TextSearchReference>> GetTextReferences(
        ConversationHistory conversationHistory,
        string collectionName,
        string tenantId,
        string language,
        string referenceType,
        string query,
        float[] vector,
        CancellationToken cancellationToken = default)
    {
        var request = GetByPromptFiltered.Request(new GetByPromptFiltered.WebsitePageQueryParams(
                collectionName,
                tenantId,
                language,
                referenceType,
                query,
                vector,
                conversationHistory.AmountOfSearchReferences)
            );

        var search = await _vectorizationService
            .SearchAsync<GetByPromptFiltered.WebsitePageQueryParams, GetByPromptFiltered.WeaviateRecordResponse>(GetByPromptFiltered.Key, request, cancellationToken);

        return search
            .GroupBy(p => p.Source)
            .Select(grouping =>
            {
                var first = grouping.First();

                first.Text = string.Join(" ", grouping.Select(g => g.Text).ToList());

                return first;
            })
            .Select(result =>
            {
                Enum.TryParse<Language>(result.Language, out var lang);

                return new TextSearchReference
                {
                    Content = result.Text,
                    Source = result.Source,
                    Title = result.Title,
                    Type = result.ReferenceType,
                    Certainty = result.Additional?.Certainty,
                    Language = lang,
                    InternalId = result.InternalId,
                    ArticleNumber = result.ArticleNumber,
                    Packaging = result.Packaging,
                };
            })
            .ToList();
    }

    private static string GetCacheKey(Guid conversationId) =>
        $"conversation_{conversationId}";

    [GeneratedRegex("\\[([0-9]*?)\\]")]
    private static partial Regex SourceIndexRegex();


}