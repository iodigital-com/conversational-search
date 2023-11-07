using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Exceptions;
using ConversationalSearchPlatform.BackOffice.Extensions;
using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Resources;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Queries;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Microsoft.Extensions.Caching.Memory;
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

        var (chatBuilder, textReferences, imageReferences) = await BuildChatAsync(holdConversation, conversationHistory, cancellationToken);

        var chatResult = await chatBuilder.ExecuteAndCalculateCostAsync(false, cancellationToken);
        _telemetryService.RegisterGPTUsage(
            holdConversation.ConversationId,
            holdConversation.TenantId,
            chatResult.Result.Usage ?? throw new InvalidOperationException("No usage was passed in after executing an OpenAI call"),
            conversationHistory.Model
        );

        var answer = chatResult.Result.CombineAnswers();

        conversationHistory.AppendToConversation(holdConversation.UserPrompt, answer);

        var conversationReferencedResult = ParseAnswerWithReferences(holdConversation, conversationHistory, textReferences, imageReferences);

        conversationHistory.SaveConversationHistory(_conversationsCache, cacheKey);
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

        var (chatBuilder, textReferences, imageReferences) = await BuildChatAsync(holdConversation, conversationHistory, cancellationToken);

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

        var result = ParseAnswerWithReferences(holdConversation, conversationHistory, textReferences, imageReferences);

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

        var vector = await _vectorizationService.CreateVectorAsync(holdConversation.ConversationId, holdConversation.TenantId, UsageType.Conversation, holdConversation.UserPrompt);
        var textReferences = await GetTextReferences(
            conversationHistory,
            nameof(WebsitePage),
            tenantId,
            holdConversation.Language.ToString(),
            ConversationReferenceType.Manual.ToString(), // TODO later extend this to accept multiple kind of references
            vector,
            cancellationToken);

        var imageReferences = await GetImageReferences(
            conversationHistory,
            IndexingConstants.ImageClass,
            holdConversation.UserPrompt,
            textReferences.Select(reference => reference.InternalId).Distinct().ToList(),
            cancellationToken);

        var indexedTextReferences = IndexizeTextReferences(textReferences);

        var systemPrompt = promptBuilder
            .ReplaceTextSources(FlattenTextReferences(indexedTextReferences))
            .ReplaceImageSources(FlattenImageReferences(indexedTextReferences, imageReferences))
            .Build();

        var chatBuilder = _openAiFactory.CreateChat()
            .RequestWithSystemMessage(systemPrompt)
            .AddPreviousMessages(conversationHistory.PromptResponses)
            .AddUserMessage(holdConversation.UserPrompt)
            .WithModel((ChatModelType)conversationHistory.Model)
            .WithTemperature(1);

        if (conversationHistory.DebugEnabled)
        {
            conversationHistory.AppendPreRequestDebugInformation(chatBuilder, textReferences, imageReferences, promptBuilder);
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

    private static List<SortedSearchReference> IndexizeTextReferences(List<TextSearchReference> textReferences)
    {
        return textReferences.Select((textRef, i) => new SortedSearchReference
            {
                TextSearchReference = textRef,
                Index = i + 1
            })
            .ToList();
    }

    private static ConversationReferencedResult ParseAnswerWithReferences(HoldConversation holdConversation,
        ConversationHistory conversationHistory,
        IReadOnlyCollection<SortedSearchReference> textReferences,
        List<ImageSearchReference> imageReferences)
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
                var sortedSearchReference = textReferences.First(reference => reference.Index == parsedIdx);
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
            knowledgeBaseBuilder.AppendLine($"{reference.Index} | {reference.TextSearchReference.Source} | {reference.TextSearchReference.Content.ReplaceLineEndings(" ")}");
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
        var request = new GetImagesFiltered()
            .Request(new GetImagesFiltered.ImageQueryParams(
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
        float[] vector,
        CancellationToken cancellationToken = default)
    {
        var request = new GetByPromptFiltered()
            .Request(new GetByPromptFiltered.WebsitePageQueryParams(
                collectionName,
                tenantId,
                language,
                referenceType,
                vector,
                conversationHistory.AmountOfSearchReferences)
            );

        var search = await _vectorizationService
            .SearchAsync<GetByPromptFiltered.WebsitePageQueryParams, GetByPromptFiltered.WeaviateRecordResponse>(GetByPromptFiltered.Key, request, cancellationToken);

        return search
            .GroupBy(p => p.Source)
            .Select(grouping => grouping.First())
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
                    InternalId = result.InternalId
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