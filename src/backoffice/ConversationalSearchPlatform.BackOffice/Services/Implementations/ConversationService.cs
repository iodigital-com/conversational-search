using System.Runtime.CompilerServices;
using System.Text;
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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
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
        IOpenAIUsageTelemetryService telemetryService)
    {
        _conversationsCache = conversationsCache;
        _openAiFactory = openAiFactory;
        _tenantStore = tenantStore;
        _vectorizationService = vectorizationService;
        _logger = logger;
        _telemetryService = telemetryService;
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

        bool shouldEndConversation = false;

        // are we at the maximum number of follow up questions after this one?
        if (conversationHistory.PromptResponses.Count > 2)
        {
            shouldEndConversation = true;
        }

        var (chatBuilder, textReferences, imageReferences, productReferences) = await BuildChatAsync(holdConversation, conversationHistory, cancellationToken);

        string answer = string.Empty;
        // don't give an answer when no references are found
        if (textReferences.Count == 0 && productReferences.Count == 0 && imageReferences.Count == 0)
        {
            shouldEndConversation = true;

            answer = "I'm sorry, but I couldn't find relevant information in my database. Try asking a new question, please.";
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

            answer = chatResult.Result.CombineAnswers();
        }

        conversationHistory.HasEnded = shouldEndConversation;
        conversationHistory.AppendToConversation(holdConversation.UserPrompt, answer);
        conversationHistory.SaveConversationHistory(_conversationsCache, cacheKey);

        textReferences.AddRange(productReferences);
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

        var (chatBuilder, textReferences, imageReferences, productReferences) = await BuildChatAsync(holdConversation, conversationHistory, cancellationToken);

        await foreach (var entry in chatBuilder
                           .ExecuteAsStreamAndCalculateCostAsync(false, cancellationToken)
                           .SelectAwait(streamEntry => ProcessStreamedChatChunk(
                               holdConversation,
                               streamEntry,
                               cacheKey,
                               conversationHistory,
                               textReferences,
                               imageReferences,
                               cancellationToken)
                           )
                           .Where(result => result is { IsOk: true, Value: not null })
                           .Select(result => result.Value!)
                           .WithCancellation(cancellationToken))
        {
            yield return entry;
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


    private ValueTask<StreamResult<ConversationReferencedResult>> ProcessStreamedChatChunk(HoldConversation holdConversation,
        CostResult<StreamingChatResult> streamEntry,
        string cacheKey,
        ConversationHistory conversationHistory,
        List<SortedSearchReference> textReferences,
        List<ImageSearchReference> imageReferences,
        CancellationToken cancellationToken)
    {
        var chunk = streamEntry.Result.LastChunk.Choices?
            .Select(choice => choice)
            .FirstOrDefault(choice => choice.Delta is { Role: ChatRole.Assistant });

        if (chunk == null)
        {
            return ValueTask.FromResult(StreamResult<ConversationReferencedResult>.Skip("NoAssistantRole"));
        }

        var completed = chunk.IsAnswerCompleted(_logger);

        var streamCancelledOrFinished = StreamCancelledOrFinished(completed, cancellationToken) && HasFullyComposedMessage(streamEntry);

        if (streamCancelledOrFinished)
        {
            var answer = streamEntry.Result.CombineStreamAnswer();
            conversationHistory.IsStreaming = false;
            conversationHistory.StreamingResponseChunks.Clear();

            conversationHistory.AppendToConversation(holdConversation.UserPrompt, answer);
            conversationHistory.SaveConversationHistory(_conversationsCache, cacheKey);
        }
        else
        {
            var chunkedAnswer = chunk.Delta?.Content ?? string.Empty;
            conversationHistory.StreamingResponseChunks.Add(chunkedAnswer);
        }

        var result = ParseAnswerWithReferences(holdConversation, conversationHistory, textReferences, imageReferences, false);

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

    private async Task<(ChatRequestBuilder chatRequestBuilder, List<SortedSearchReference>, List<ImageSearchReference>, List<SortedSearchReference>)> BuildChatAsync(
        HoldConversation holdConversation,
        ConversationHistory conversationHistory,
        CancellationToken cancellationToken)
    {
        var tenantId = holdConversation.TenantId;
        var tenant = await GetTenantAsync(tenantId);

        if (conversationHistory.DebugEnabled)
        {
            conversationHistory.InitializeDebugInformation();
        }

        holdConversation.UserPrompt = holdConversation.UserPrompt.Trim();

        var promptBuilder = new PromptBuilder(new StringBuilder((await GetEmbeddedResourceText(ResourceConstants.BasePromptFile)).Trim()))
            .ReplaceTenantPrompt(GetTenantPrompt(tenant))
            .ReplaceConversationContextVariables(tenant.PromptTags ?? new List<PromptTag>(), holdConversation.ConversationContext);

        var vectorPrompt = new StringBuilder();

        if (!conversationHistory.PromptResponses.IsNullOrEmpty())
        {
            vectorPrompt.AppendLine(conversationHistory.PromptResponses.Last().response);
        }

        vectorPrompt.AppendLine(holdConversation.UserPrompt);

        var vector = await _vectorizationService.CreateVectorAsync(holdConversation.ConversationId, holdConversation.TenantId, UsageType.Conversation, vectorPrompt.ToString());
        
        var textReferences = await GetTextReferences(
            conversationHistory,
            nameof(WebsitePage),
            tenantId,
            "English",
            ConversationReferenceType.Site.ToString(), // TODO later extend this to accept multiple kind of references
            vectorPrompt.ToString(),
            vector,
            cancellationToken);

        var productReferences = await GetTextReferences(
            conversationHistory,
            nameof(WebsitePage),
            tenantId,
            "English",
            ConversationReferenceType.Product.ToString(), // TODO later extend this to accept multiple kind of references
            vectorPrompt.ToString(),
            vector,
            cancellationToken);


        var articleNumber = Regex.Match(holdConversation.UserPrompt, @"\d+").Value;

        if (!string.IsNullOrEmpty(articleNumber))
        {
            if (!productReferences.Any(p => p.ArticleNumber == articleNumber))
            {
                var articleNumberReferences = await GetProductReferenceById(articleNumber,
                    nameof(WebsitePage),
                    tenantId,
                    "English",
                    ConversationReferenceType.Product.ToString(),
                    cancellationToken);

                productReferences.AddRange(articleNumberReferences);
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

        var indexedTextReferences = IndexizeTextReferences(textReferences);

        var startIndex = 0;

        if (!indexedTextReferences.IsNullOrEmpty())
        {
            startIndex = indexedTextReferences.Last().Index;
        }

        var indexedProductReferences = IndexizeTextReferences(productReferences, startIndex);

        var systemPrompt = promptBuilder
            .ReplaceTextSources(FlattenTextReferences(indexedTextReferences))
            .ReplaceProductSources(FlattenProductReferences(indexedProductReferences))
            .Build();

        var chatModel = (ChatModelType)conversationHistory.Model;
        var chatBuilder = _openAiFactory.CreateChat()
            .RequestWithSystemMessage(systemPrompt)
            .AddPreviousMessages(conversationHistory.PromptResponses)
            .AddUserMessage(holdConversation.UserPrompt)
            .WithModel(chatModel)
            .WithTemperature(0.75);

        if (conversationHistory.DebugEnabled)
        {
            conversationHistory.AppendPreRequestDebugInformation(chatBuilder, textReferences, imageReferences, promptBuilder);
        }

        return (chatBuilder, indexedTextReferences, imageReferences, indexedProductReferences);
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

    private static List<SortedSearchReference> IndexizeTextReferences(List<TextSearchReference> textReferences, int startIndex = 0)
    {
        return textReferences.Select((textRef, i) => new SortedSearchReference
            {
                TextSearchReference = textRef,
                Index = i + 1 + startIndex
            })
            .ToList();
    }

    private static ConversationReferencedResult ParseAnswerWithReferences(HoldConversation holdConversation,
        ConversationHistory conversationHistory,
        IReadOnlyCollection<SortedSearchReference> textReferences,
        List<ImageSearchReference> imageReferences,
        bool endConversation)
    {
        string mergedAnswer;

        var isStreaming = conversationHistory.IsStreaming;

        if (isStreaming)
        {
            mergedAnswer = conversationHistory.GetAllStreamingResponseChunksMerged();
        }
        else
        {
            var (_, answer) = conversationHistory.PromptResponses.Last();
            mergedAnswer = answer;
        }

        var validReferences = DetermineValidReferences(textReferences, mergedAnswer);

        if (conversationHistory.DebugEnabled)
        {
            conversationHistory.AppendPostRequestTextReferenceDebugInformation(validReferences);
            conversationHistory.AppendPostRequestImageReferenceDebugInformation(imageReferences, mergedAnswer);
        }

        return new ConversationReferencedResult(
            new ConversationResult(
                holdConversation.ConversationId,
                isStreaming ? conversationHistory.StreamingResponseChunks.Last() : mergedAnswer,
                holdConversation.Language
            ),
            validReferences,
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

    private static string FlattenTextReferences(List<SortedSearchReference> references)
    {
        var knowledgeBaseBuilder = new StringBuilder();

        foreach (var reference in references)
        {
            knowledgeBaseBuilder.AppendLine($"{reference.Index} | {reference.TextSearchReference.Content.ReplaceLineEndings(" ")}");
        }

        return knowledgeBaseBuilder.ToString();
    }

    private static string FlattenProductReferences(List<SortedSearchReference> references)
    {
        var knowledgeBaseBuilder = new StringBuilder();

        foreach (var reference in references)
        {
            knowledgeBaseBuilder.AppendLine($"{reference.Index} | {reference.TextSearchReference.ArticleNumber} | {reference.TextSearchReference.Packaging} | {reference.TextSearchReference.Title} | {reference.TextSearchReference.Content.ReplaceLineEndings(" ")}");
        }

        return knowledgeBaseBuilder.ToString();
    }

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
            .Select(grouping => {
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
                    Type = refType,
                    Certainty = result.Additional?.Certainty,
                    Language = lang,
                    InternalId = result.InternalId,
                    ArticleNumber = result.ArticleNumber,
                    Packaging = result.Packaging,
                };
            })
            .ToList();
    }

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
            .Select(grouping => {
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
                    Type = refType,
                    Certainty = result.Additional?.Certainty,
                    Language = lang,
                    InternalId = result.InternalId,
                    ArticleNumber = result.ArticleNumber,
                    Packaging = result.Packaging,
                };
            })
            .ToList();
    }

    private async Task<string> GetEmbeddedResourceText(string resourceName)
    {
        var resourceContents = string.Empty;
        var fullResourceName = $"ConversationalSearchPlatform.BackOffice.Resources.{resourceName}";
        var assembly = this.GetType().Assembly;

        using var stream = assembly.GetManifestResourceStream(fullResourceName);

        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            resourceContents = (await reader.ReadToEndAsync()).Trim();
        }

        return resourceContents;
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