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
                            "type": "string"
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

        conversationHistory.AppendToConversation(holdConversation.UserPrompt);
        conversationHistory.AppendToConversation(new UserChatMessage("Do not give me any information that is not mentioned in the <SOURCES> document or in the tenant prompt <TenantPrompt>. Only use the functions you have been provided with."));
            

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
                                        indexedTextReferences = new List<SortedSearchReference>();
                                        var index = 0;

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
                                                    ReferenceId = $"{index}",
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

                                        Console.WriteLine($"Sources: {sources}");


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

        var conversationReferencedResult = ParseAnswerWithReferences(holdConversation, conversationHistory, indexedTextReferences, toolImageReferences, shouldEndConversation);

        return conversationReferencedResult;
    }

    public async IAsyncEnumerable<ConversationReferencedResult> ConverseStreamingAsync(
        HoldConversation holdConversation,
        string tenantId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {


        yield return new ConversationReferencedResult(new ConversationResult(Guid.NewGuid(), "", Language.Dutch), new List<ConversationReference>(), true, null);
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


    /*private ValueTask<StreamResult<ConversationReferencedResult>> ProcessStreamedChatChunk(HoldConversation holdConversation,
        CostResult<StreamingChatResult> streamEntry,
        string cacheKey,
        ConversationHistory conversationHistory,
        List<SortedSearchReference> textReferences,
        List<ImageSearchReference> imageReferences,
        bool shouldEndConversation,
        ChatMessage composedMessage,
        CancellationToken cancellationToken)
    {
        var chunk = streamEntry.Result.LastChunk.Choices?
            .Select(choice => choice)
            .FirstOrDefault(choice => choice.Delta is { Role: ChatRole.Assistant });

        if (chunk == null)
        {
            return ValueTask.FromResult(StreamResult<ConversationReferencedResult>.Skip("NoAssistantRole"));
        }

        _logger.LogInformation("Chunk: " + (chunk.Delta?.Content ?? ""));
        _logger.LogInformation("Chunk finish reason: " + (chunk.FinishReason ?? "not finished"));

        var completed = !string.IsNullOrWhiteSpace(chunk.FinishReason);

        var streamCancelledOrFinished = StreamCancelledOrFinished(completed, cancellationToken) && HasFullyComposedMessage(streamEntry);

        ConversationReferencedResult? result = null;

        if (streamCancelledOrFinished)
        {
            var chunkedAnswer = chunk.Delta?.Content ?? string.Empty;
            conversationHistory.StreamingResponseChunks.Add(chunkedAnswer);
            conversationHistory.IsLastChunk = true;

            // function call or not?
            var composedResult = streamEntry.Result.Composed;
            var message = streamEntry.Result.Composed.GetFirstAnswer();
            if (message.Function == null)
            {
                result = ParseAnswerWithReferences(holdConversation, conversationHistory, textReferences, imageReferences, shouldEndConversation);

                conversationHistory.StreamingResponseChunks.Clear();
                conversationHistory.IsStreaming = false;

                conversationHistory.AppendToConversation(holdConversation.UserPrompt, message);
            }
            else
            {
                // call the function
                conversationHistory.AppendToConversation(holdConversation.UserPrompt, message);
            }

            conversationHistory.SaveConversationHistory(_conversationsCache, cacheKey);

            composedMessage.Content = message.Content;
            composedMessage.Function = message.Function;
            composedMessage.Role = message.Role;
            composedMessage.Name = message.Name;
        }
        else
        {
            if (!string.IsNullOrEmpty(chunk.Delta?.Content))
            {
                conversationHistory.IsLastChunk = false;
                var chunkedAnswer = chunk.Delta?.Content ?? string.Empty;
                conversationHistory.StreamingResponseChunks.Add(chunkedAnswer);
                result = ParseAnswerWithReferences(holdConversation, conversationHistory, textReferences, imageReferences, shouldEndConversation);
            }
            else
            {
                return ValueTask.FromResult(StreamResult<ConversationReferencedResult>.Skip("NoChunkContent"));
            }
        }

        if (conversationHistory.DebugEnabled)
        {
            conversationHistory.SaveConversationHistory(_conversationsCache, cacheKey);

            if (!streamCancelledOrFinished)
            {
                // only print debug result on last chunk
                result.DebugInformation = null;
            }
        }


        return ValueTask.FromResult(StreamResult<ConversationReferencedResult>.Ok(result));
    }*/

    private async Task<(List<ChatMessage> messages, List<SortedSearchReference>, List<ImageSearchReference>)> BuildChatAsync(
        HoldConversation holdConversation,
        ConversationHistory conversationHistory,
        CancellationToken cancellationToken)
    {
        Guid TENA_ID = Guid.Parse("CCFA9314-ABE6-403A-9E21-2B31D95A5258");
        var tenantId = holdConversation.TenantId;
        var tenant = await GetTenantAsync(tenantId);

        if (conversationHistory.DebugEnabled)
        {
            conversationHistory.InitializeDebugInformation();
        }

        holdConversation.UserPrompt = new UserChatMessage(holdConversation.UserPrompt.Content.FirstOrDefault()?.Text?.Trim() ?? string.Empty);

        var promptBuilder = new PromptBuilder(new StringBuilder((await ResourceHelper.GetEmbeddedResourceTextAsync(ResourceHelper.BasePromptFile)).Trim()))
            .ReplaceTenantPrompt(GetTenantPrompt(tenant))
            .ReplaceConversationContextVariables(tenant.PromptTags ?? new List<PromptTag>(), holdConversation.ConversationContext);

        var chatBuilder = new List<ChatMessage>
        {
            new SystemChatMessage(promptBuilder.Build())
        };
        chatBuilder.AddRange(conversationHistory.Messages);
        chatBuilder.Add(holdConversation.UserPrompt);

        if (holdConversation.UserPrompt is UserChatMessage)
        {
            chatBuilder.Add(new UserChatMessage("Do not give me any information that is not mentioned in the <SOURCES> document. Only use the functions you have been provided with."));
        }

        if (conversationHistory.DebugEnabled)
        {
            conversationHistory.AppendPreRequestDebugInformation(chatBuilder, new(), new(), promptBuilder);
        }

        return (chatBuilder, new(), new());
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

    private static ConversationReferencedResult ParseAnswerWithReferences(HoldConversation holdConversation,
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
    }


    private static List<ConversationReference> DetermineValidReferences(
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
    }

    private ConversationHistory GetConversationHistory(HoldConversation holdConversation, string cacheKey)
    {
        if (!_conversationsCache.TryGetValue(cacheKey, out ConversationHistory? conversationHistory))
        {
            ThrowHelper.ThrowConversationNotFoundException(holdConversation.ConversationId);
        }

        return conversationHistory!;
    }


    private static string GetTenantPrompt(ApplicationTenantInfo tenant) => tenant.GetBasePromptOrDefault();


    private static string FlattenImageReferences(List<SortedSearchReference> textSearchReferences, List<ImageSearchReference> references)
    {
        var knowledgeBaseBuilder = new StringBuilder();

        foreach (var reference in references)
        {
            var matching = textSearchReferences.FirstOrDefault(searchReference => searchReference.TextSearchReference.InternalId == reference.InternalId);

            if (matching == null)
            {
                continue;
            }

            knowledgeBaseBuilder.AppendLine($"{matching.Index} | {reference.Source} | {reference.AltDescription?.ReplaceLineEndings(" ")}");
        }

        return knowledgeBaseBuilder.ToString();
    }

    private async Task<List<ImageSearchReference>> GetImageReferences(
        ConversationHistory conversationHistory,
        string collectionName,
        string prompt,
        List<string> textReferenceInternalIds,
        CancellationToken cancellationToken = default)
    {
        if (textReferenceInternalIds.Count == 0)
        {
            return new List<ImageSearchReference>();
        }

        var request = GetImagesFiltered.Request(new GetImagesFiltered.ImageQueryParams(
                collectionName,
                conversationHistory.AmountOfSearchReferences,
                prompt,
                textReferenceInternalIds
            ));

        var search = await _vectorizationService
            .SearchAsync<GetImagesFiltered.ImageQueryParams, GetImagesFiltered.WeaviateRecordResponse>(GetImagesFiltered.Key, request, cancellationToken);

        return search.Select(result =>
                new ImageSearchReference
                {
                    InternalId = result.InternalId,
                    Source = result.Url,
                    AltDescription = result.AltDescription,
                    Certainty = result.Additional?.Certainty,
                })
            .ToList();
    }

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

    // specialleke voor Tena
    private async Task<List<TextSearchReference>> GetProductReferenceById(
        string articleNumber,
        string collectionName,
        string tenantId,
        string language,
        string referenceType,
        CancellationToken cancellationToken = default)
    {
        var request = new GraphQLRequest
        {
            Query = $$"""
                      {
                      	Get {
                      		{{collectionName}}(
                              where: {
                      			operator: And
                      			operands: [
                      				{ path: ["language"], operator: Equal, valueText: "{{language}}" }
                      				{ path: ["referenceType"], operator: Equal, valueText: "{{referenceType}}" }
                      				{ path: ["tenantId"], operator: Equal, valueText: "{{tenantId}}" }
                      				{ path: ["articlenumber"], operator: Equal, valueText: "{{articleNumber}}" }
                      			]
                               }
                      		) {
                      		    internalId
                      		    tenantId
                      			text
                      			title
                      			source
                      			language
                      			referenceType
                                articlenumber
                                packaging
                      			_additional {
                      	            id,
                      	            certainty,
                      	            distance
                                }
                      		}
                      	}
                      }
                      """
        };

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
                Enum.TryParse<ConversationReferenceType>(result.ReferenceType, out var refType);
                Enum.TryParse<Language>(result.Language, out var lang);

                return new TextSearchReference
                {
                    Content = result.Text,
                    Source = result.Source,
                    Title = result.Title,
                    Type = "Product",
                    Certainty = result.Additional?.Certainty,
                    Language = lang,
                    InternalId = result.InternalId,
                    ArticleNumber = result.ArticleNumber,
                    Packaging = result.Packaging,
                };
            })
            .ToList();
    }

    private static bool StreamCancelledOrFinished(bool completed, CancellationToken cancellationToken) =>
        completed || cancellationToken.IsCancellationRequested;

    private static string GetCacheKey(Guid conversationId) =>
        $"conversation_{conversationId}";

    [GeneratedRegex("\\[([0-9]*?)\\]")]
    private static partial Regex SourceIndexRegex();


}