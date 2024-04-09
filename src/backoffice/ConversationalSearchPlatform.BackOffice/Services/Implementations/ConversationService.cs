using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Exceptions;
using ConversationalSearchPlatform.BackOffice.Extensions;
using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Resources;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Queries;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using GraphQL;
using Jint;
using Jint.Fetch;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Rystem.OpenAi;
using Rystem.OpenAi.Chat;
using Language = ConversationalSearchPlatform.BackOffice.Services.Models.Language;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public partial class ConversationService : IConversationService
{
    private readonly IMemoryCache _conversationsCache;
    private readonly IOpenAiFactory _openAiFactory;
    private readonly IMultiTenantStore<ApplicationTenantInfo> _tenantStore;
    private readonly IVectorizationService _vectorizationService;
    private readonly IOpenAIUsageTelemetryService _telemetryService;
    private readonly IKeywordExtractorService _keywordExtractorService;
    private readonly IRagService _ragService;
    private readonly ILogger<ConversationService> _logger;


    private readonly MemoryCacheEntryOptions _defaultMemoryCacheEntryOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(2)
    };

    public ConversationService(
        IMemoryCache conversationsCache,
        IOpenAiFactory openAiFactory,
        IMultiTenantStore<ApplicationTenantInfo> tenantStore,
        IVectorizationService vectorizationService,
        ILogger<ConversationService> logger,
        IOpenAIUsageTelemetryService telemetryService,
        IKeywordExtractorService keywordExtractorService,
        IRagService ragService)
    {
        _conversationsCache = conversationsCache;
        _openAiFactory = openAiFactory;
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

        bool shouldEndConversation = false;

        // are we at the maximum number of follow up questions after this one?
        if (conversationHistory.PromptResponses.Count >= 5)
        {
            shouldEndConversation = true;
        }

        var (chatBuilder, textReferences, imageReferences) = await BuildChatAsync(holdConversation, conversationHistory, cancellationToken);

        ChatMessage answer = new ChatMessage() { Role = ChatRole.Assistant };
        // don't give an answer when no references are found
        if (textReferences.Count == 0 && imageReferences.Count == 0)
        {
            shouldEndConversation = true;

            answer.Content = "I'm sorry, but I couldn't find relevant information in my database. Try asking a new question, please.";
        }
        else
        {
            var chatResult = await chatBuilder.ExecuteAndCalculateCostAsync(false, cancellationToken);
            _telemetryService.RegisterGPTUsage(
                holdConversation.ConversationId,
                holdConversation.TenantId,
                chatResult.Result.Usage ?? throw new InvalidOperationException("No usage was passed in after executing an OpenAI call"),
                conversationHistory.Model
            );

            answer = chatResult.Result.GetFirstAnswer();
            if (answer.Function != null)
            {
                // call the function
                conversationHistory.AppendToConversation(holdConversation.UserPrompt, answer);

                // add the function reply
                var (functionReply, keywordString) = await Task<string>.Run(() => {

                    var functionReplyTask = CallFunction(answer.Function.Name, answer.Function.Arguments);

                    return functionReplyTask;
                }).ConfigureAwait(false);

                // create new chatbuilder request with product reference
                ChatMessage functionMessage = new ChatMessage()
                {
                    Role = ChatRole.Function,
                    Content = functionReply,
                    Name = answer.Function.Name,
                };

                holdConversation.UserPrompt = functionMessage;
                (chatBuilder, textReferences, imageReferences) = await BuildChatAsync(holdConversation, conversationHistory, cancellationToken);
                chatResult = await chatBuilder.ExecuteAndCalculateCostAsync(false, cancellationToken);
                    _telemetryService.RegisterGPTUsage(
                    holdConversation.ConversationId,
                    holdConversation.TenantId,
                    chatResult.Result.Usage ?? throw new InvalidOperationException("No usage was passed in after executing an OpenAI call"),
                    conversationHistory.Model
                );
                answer = chatResult.Result.GetFirstAnswer();
                conversationHistory.AppendToConversation(functionMessage, answer);
            }
            else
            {
                conversationHistory.AppendToConversation(holdConversation.UserPrompt, answer);
            }
        }

        conversationHistory.HasEnded = shouldEndConversation;
        conversationHistory.SaveConversationHistory(_conversationsCache, cacheKey);

        var conversationReferencedResult = ParseAnswerWithReferences(holdConversation, conversationHistory, textReferences, imageReferences, shouldEndConversation);

        return conversationReferencedResult;
    }

    public async IAsyncEnumerable<ConversationReferencedResult> ConverseStreamingAsync(
        HoldConversation holdConversation,
        string tenantId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(holdConversation.ConversationId);
        var conversationHistory = GetConversationHistory(holdConversation, cacheKey);
        conversationHistory.IsStreaming = true;
        conversationHistory.DebugEnabled = holdConversation.Debug;
        bool shouldEndConversation = false;

        //todo: this is a bit of code repetition
        // are we at the maximum number of follow up questions after this one?
        if (conversationHistory.PromptResponses.Count >= 5)
        {
            shouldEndConversation = true;
        }

        var (chatBuilder, textReferences, imageReferences) = await BuildChatAsync(holdConversation, conversationHistory, cancellationToken);

        string answer = string.Empty;
        // don't give an answer when no references are found
        if (textReferences.Count == 0 && imageReferences.Count == 0)
        {
            shouldEndConversation = true;
            answer = "I'm sorry, but I couldn't find relevant information in my database. Try asking a new question, please.";
        }

        conversationHistory.HasEnded = shouldEndConversation;
        ChatMessage composedMessage = new ChatMessage();

        await foreach (var entry in chatBuilder
                           .ExecuteAsStreamAndCalculateCostAsync(false, cancellationToken)
                           .SelectAwait(streamEntry => ProcessStreamedChatChunk(
                               holdConversation,
                               streamEntry,
                               cacheKey,
                               conversationHistory,
                               textReferences,
                               imageReferences,
                               shouldEndConversation,
                               composedMessage,
                               cancellationToken)
                           )
                           .Where(result => result is { IsOk: true, Value: not null })
                           .Select(result => result.Value!)
                           .WithCancellation(cancellationToken))
        {
            yield return entry;
        }

        if (composedMessage.Function != null)
        {
            // add the function reply
            var (functionReply, keywordString) = await Task<string>.Run(() => {

                var functionReplyTask = CallFunction(composedMessage.Function.Name, composedMessage.Function.Arguments);

                return functionReplyTask;
            }).ConfigureAwait(false);

            // create new chatbuilder request with product reference
            ChatMessage functionMessage = new ChatMessage()
            {
                Role = ChatRole.Function,
                Content = functionReply,
                Name = composedMessage.Function.Name,
            };

            holdConversation.UserPrompt = functionMessage;
            (chatBuilder, textReferences, imageReferences) = await BuildChatAsync(holdConversation, conversationHistory, cancellationToken);

            await foreach (var entry in chatBuilder
                           .ExecuteAsStreamAndCalculateCostAsync(false, cancellationToken)
                           .SelectAwait(streamEntry => ProcessStreamedChatChunk(
                               holdConversation,
                               streamEntry,
                               cacheKey,
                               conversationHistory,
                               textReferences,
                               imageReferences,
                               shouldEndConversation,
                               composedMessage,
                               cancellationToken)
                           )
                           .Where(result => result is { IsOk: true, Value: not null })
                           .Select(result => result.Value!)
                           .WithCancellation(cancellationToken))
            {
                yield return entry;
            }
        }
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


    private ValueTask<StreamResult<ConversationReferencedResult>> ProcessStreamedChatChunk(HoldConversation holdConversation,
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
    }

    private async Task<(ChatRequestBuilder chatRequestBuilder, List<SortedSearchReference>, List<ImageSearchReference>)> BuildChatAsync(
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

        holdConversation.UserPrompt.Content = holdConversation.UserPrompt.Content?.Trim() ?? "";

        var promptBuilder = new PromptBuilder(new StringBuilder((await ResourceHelper.GetEmbeddedResourceTextAsync(ResourceHelper.BasePromptFile)).Trim()))
            .ReplaceTenantPrompt(GetTenantPrompt(tenant))
            .ReplaceConversationContextVariables(tenant.PromptTags ?? new List<PromptTag>(), holdConversation.ConversationContext);

        var vectorPrompt = new StringBuilder();

        // add history of conversation to vector context
        foreach (var promptResponse in conversationHistory.PromptResponses.TakeLast(2))
        {
            vectorPrompt.AppendLine(promptResponse.Prompt.Content);

            if (!string.IsNullOrEmpty(promptResponse.Response.Content))
            {
                vectorPrompt.AppendLine(promptResponse.Response.Content);
            }
        }

        // add last user prompt
        vectorPrompt.AppendLine(holdConversation.UserPrompt.Content);
        // convert to keywords
        var keywords = await _keywordExtractorService.ExtractKeywordAsync(vectorPrompt.ToString());

        var vector = await _vectorizationService.CreateVectorAsync(holdConversation.ConversationId, holdConversation.TenantId, UsageType.Conversation, string.Join(' ', keywords));

        var ragDocument = await _ragService.GetRAGDocumentAsync(Guid.Parse(tenantId));

        List<TextSearchReference> references = new List<TextSearchReference>();
        List<SortedSearchReference> indexedTextReferences = new List<SortedSearchReference>();
        var index = 0;

        foreach (var ragClass in ragDocument.Classes)
        {
            var textReferences = await GetTextReferences(
                conversationHistory,
                nameof(WebsitePage),
                tenantId,
                "English",
                ragClass.Name,
                vectorPrompt.ToString(),
                vector,
                cancellationToken);

            foreach (var reference in textReferences)
            {
                index++;

                Dictionary<string, string> properties = new Dictionary<string, string>();
                properties["Content"] = reference.Content;
                properties["Title"] = reference.Title;

                if (ragClass.Name == "Product" && Guid.Parse(tenantId) == TENA_ID)
                {
                    properties["ArticleNumber"] = reference.ArticleNumber;
                    properties["Packaging"] = reference.Packaging;
                }

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

            references.AddRange(textReferences);
        }

        if (Guid.Parse(tenantId) == TENA_ID)
        {
            // specialleke voor tena
            var articleNumber = Regex.Match(holdConversation.UserPrompt.Content ?? "", @"\d+").Value;

            if (!string.IsNullOrEmpty(articleNumber))
            {
                var ragClass = ragDocument.Classes.FirstOrDefault(r => r.Name == "Product");

                if (ragClass != null)
                {
                    if (!ragClass.Sources.Any(p => p.Properties["ArticleNumber"] == articleNumber))
                    {
                        var articleNumberReferences = await GetProductReferenceById(articleNumber,
                            nameof(WebsitePage),
                            tenantId,
                            "English",
                            ConversationReferenceType.Product.ToString(),
                            cancellationToken);

                        foreach (var reference in articleNumberReferences)
                        {
                            index++;

                            Dictionary<string, string> properties = new Dictionary<string, string>();
                            properties["Content"] = reference.Content;
                            properties["Title"] = reference.Title;

                            if (ragClass.Name == "Product")
                            {
                                properties["ArticleNumber"] = reference.ArticleNumber;
                                properties["Packaging"] = reference.Packaging;
                            }

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
                    }
                }
            }
        }

        // TODO: restore this, but better
        /*var imageReferences = await GetImageReferences(
            conversationHistory,
            IndexingConstants.ImageClass,
            holdConversation.UserPrompt,
            textReferences.Select(reference => reference.InternalId).Distinct().ToList(),
            cancellationToken);*/
        var imageReferences = new List<ImageSearchReference>();

        var ragString = await ragDocument.GenerateXMLStringAsync();

        var systemPrompt = promptBuilder
            .ReplaceRAGDocument(ragString)
            .Build();

        var chatModel = (ChatModelType)conversationHistory.Model;
        var chatBuilder = _openAiFactory.CreateChat()
            .RequestWithSystemMessage(systemPrompt)
            .AddPreviousMessages(conversationHistory.PromptResponses)
            .AddMessage(holdConversation.UserPrompt);

        if (holdConversation.UserPrompt.Role == ChatRole.User)
        {
            chatBuilder.AddUserMessage("Do not give me any information that is not mentioned in the <SOURCES> document. Only use the functions you have been provided with.");
        }

        chatBuilder.WithModel(chatModel)
            .WithTemperature(0.75);

        if (Guid.Parse(tenantId) == TENA_ID)
        {
            chatBuilder.WithFunction(new System.Text.Json.Serialization.JsonFunction()
            {
                Name = "get_product_recommendation",
                Description = "Get a recommendation for a Tena incontenince product based on gender, level of incontinence and mobility",
                Parameters = new JsonFunctionNonPrimitiveProperty()
                .AddEnum("gender", new JsonFunctionEnumProperty
                {
                    Type = "string",
                    Enums = new List<string> { "male", "female" },
                    Description = "The gender of the person derived from the context, male (he/him) or female (she/her)",
                })
                .AddEnum("mobility", new JsonFunctionEnumProperty
                {
                    Type = "string",
                    Enums = new List<string> { "mobile", "needs_help_toilet", "bedridden" },
                    Description = "The level of mobility of the incontinent person from mobile and being able to go to the toilet himself to bedridden",
                })
                .AddEnum("incontinence_level", new JsonFunctionEnumProperty
                {
                    Type = "string",
                    Enums = new List<string> { "small", "light", "moderate", "heavy", "very_heavy" },
                    Description = "How heavy the urine loss is, from very small drops to a full cup of urine loss. Ranging between; Small (drops making the underwear damp), Light (leakages making the underwear fairly wet), moderate (Quarter cup, making the underwear quite wet), heavy (Half cup, making the underwear very wet) and very heavy (Full cup, emptying more than half bladder). Do not make this data up, it should be explicetely mentioned by the user.",
                })
                .AddRequired("gender", "mobility", "incontinence_level")
            });
        }

        if (conversationHistory.DebugEnabled)
        {
            conversationHistory.AppendPreRequestDebugInformation(chatBuilder, references, imageReferences, promptBuilder);
        }

        return (chatBuilder, indexedTextReferences, imageReferences);
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
            var (_, answer, _, _) = conversationHistory.PromptResponses.Last();
            mergedAnswer = answer.Content;
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

    private static bool HasFullyComposedMessage(CostResult<StreamingChatResult> streamEntry) =>
        streamEntry.Result.Composed.Choices != null &&
        streamEntry.Result.Composed.Choices.Count != 0;

    private static bool StreamCancelledOrFinished(bool completed, CancellationToken cancellationToken) =>
        completed || cancellationToken.IsCancellationRequested;

    private static string GetCacheKey(Guid conversationId) =>
        $"conversation_{conversationId}";

    [GeneratedRegex("\\[([0-9]*?)\\]")]
    private static partial Regex SourceIndexRegex();


}