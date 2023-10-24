using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Exceptions;
using ConversationalSearchPlatform.BackOffice.Extensions;
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
        ILogger<ConversationService> logger)
    {
        _conversationsCache = conversationsCache;
        _openAiFactory = openAiFactory;
        _tenantStore = tenantStore;
        _vectorizationService = vectorizationService;
        _logger = logger;
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

        var (chatBuilder, textReferences) = await BuildChatAsync(holdConversation, conversationHistory, cancellationToken);

        var chatResult = await chatBuilder.ExecuteAndCalculateCostAsync(false, cancellationToken);
        var answer = chatResult.Result.CombineAnswers();

        conversationHistory.AppendToConversation(holdConversation.Prompt, answer);
        conversationHistory.SaveConversationHistory(_conversationsCache, cacheKey);

        return ParseAnswerWithReferences(holdConversation, conversationHistory, textReferences);
    }

    public async IAsyncEnumerable<ConversationReferencedResult> ConverseStreamingAsync(
        HoldConversation holdConversation,
        string tenantId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(holdConversation.ConversationId);
        var conversationHistory = GetConversationHistory(holdConversation, cacheKey);
        conversationHistory.IsStreaming = true;

        var (chatBuilder, textReferences) = await BuildChatAsync(holdConversation, conversationHistory, cancellationToken);

        await foreach (var entry in chatBuilder
                           .ExecuteAsStreamAndCalculateCostAsync(false, cancellationToken)
                           .SelectAwait(streamEntry => ProcessStreamedChatChunk(
                               holdConversation,
                               streamEntry,
                               cacheKey,
                               conversationHistory,
                               textReferences,
                               cancellationToken)
                           )
                           .Where(result => result is { IsOk: true, Value: not null })
                           .Select(result => result.Value!)
                           .WithCancellation(cancellationToken))
        {
            yield return entry;
        }
    }

    public async Task<ConversationSimulation> SimulateAsync(HoldConversation holdConversation, CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(holdConversation.ConversationId);
        var conversationHistory = GetConversationHistory(holdConversation, cacheKey);

        var (chatBuilder, textReferences) = await BuildChatAsync(holdConversation, conversationHistory, cancellationToken);
        var fullPrompt = string.Join(Environment.NewLine, chatBuilder.GetCurrentMessages().Select(message => message.Content));
        return new ConversationSimulation(fullPrompt);
    }

    private ValueTask<StreamResult<ConversationReferencedResult>> ProcessStreamedChatChunk(
        HoldConversation holdConversation,
        CostResult<StreamingChatResult> streamEntry,
        string cacheKey,
        ConversationHistory conversationHistory,
        List<SortedSearchReference> textReferences,
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

        if (StreamCancelledOrFinished(completed, cancellationToken) && HasFullyComposedMessage(streamEntry))
        {
            var answer = streamEntry.Result.CombineStreamAnswer();
            conversationHistory.IsStreaming = false;
            conversationHistory.StreamingResponseChunks.Clear();

            conversationHistory.AppendToConversation(holdConversation.Prompt, answer);
            conversationHistory.SaveConversationHistory(_conversationsCache, cacheKey);
        }
        else
        {
            var chunkedAnswer = chunk.Delta?.Content ?? string.Empty;
            conversationHistory.StreamingResponseChunks.Add(chunkedAnswer);
        }

        var result = ParseAnswerWithReferences(holdConversation, conversationHistory, textReferences);
        return ValueTask.FromResult(StreamResult<ConversationReferencedResult>.Ok(result));
    }

    private async Task<(ChatRequestBuilder chatRequestBuilder, List<SortedSearchReference>)> BuildChatAsync(
        HoldConversation holdConversation,
        ConversationHistory conversationHistory,
        CancellationToken cancellationToken)
    {
        holdConversation.Prompt = holdConversation.Prompt.Trim();
        var basePrompt = (await GetEmbeddedResourceText(ResourceConstants.BasePromptFile)).Trim();

        var tenantPrompt = await GetTenantPromptAsync(holdConversation.TenantId, holdConversation.ConversationContext);
        basePrompt = basePrompt.Replace("{{TenantPrompt}}", tenantPrompt);

        var vector = await _vectorizationService.CreateVectorAsync(holdConversation.Prompt);
        var textReferences = await GetTextReferences(
            conversationHistory,
            nameof(WebsitePage),
            holdConversation.TenantId,
            holdConversation.Language.ToString(),
            ConversationReferenceType.Official.ToString(), // TODO later extend this to accept multiple kind of references
            vector,
            cancellationToken);

        var imageSearchReferences = await GetImageReferences(
            conversationHistory,
            IndexingConstants.ImageClass,
            holdConversation.Prompt,
            textReferences.Select(reference => reference.InternalId).Distinct().ToList(),
            cancellationToken);

        var indexedTextReferences = IndexizeTextReferences(textReferences);

        basePrompt = basePrompt.Replace("{{TextSources}}", FlattenTextReferences(indexedTextReferences));
        basePrompt = basePrompt.Replace("{{ImageSources}}", FlattenImageReferences(indexedTextReferences, imageSearchReferences));

        var chatBuilder = _openAiFactory.CreateChat()
            .RequestWithSystemMessage(basePrompt)
            .AddPreviousMessages(conversationHistory.PromptResponses)
            .AddUserMessage(holdConversation.Prompt)
            .WithModel((ChatModelType)conversationHistory.Model)
            .WithTemperature(1);

        return (chatBuilder, indexedTextReferences);
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

    private static ConversationReferencedResult ParseAnswerWithReferences(
        HoldConversation holdConversation,
        ConversationHistory conversationHistory,
        IReadOnlyCollection<SortedSearchReference> textReferences)
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

        return new ConversationReferencedResult(
            new ConversationResult(
                holdConversation.ConversationId,
                isStreaming ? conversationHistory.StreamingResponseChunks.Last() : mergedAnswer,
                holdConversation.Language
            ),
            validReferences);
    }

    private static List<ConversationReference> DetermineValidReferences(IReadOnlyCollection<SortedSearchReference> textReferences, string mergedAnswer)
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
                var reference = new ConversationReference(parsedIdx, sortedSearchReference.TextSearchReference.Source, sortedSearchReference.TextSearchReference.Type);

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


    private async Task<string> GetTenantPromptAsync(string tenantId, IDictionary<string, string> conversationContext)
    {
        var tenant = await _tenantStore.TryGetAsync(tenantId);

        if (tenant == null)
        {
            ThrowHelper.ThrowTenantNotFoundException(tenantId);
        }

        var prompt = tenant.GetBasePromptOrDefault();

        var promptTags = tenant.PromptTags ?? new List<PromptTag>();

        foreach (var promptTag in promptTags)
        {
            var trimmedKey = promptTag.Value.TrimStart('{').TrimEnd('}');
            conversationContext.TryGetValue(trimmedKey, out var value);

            if (value != null)
            {
                prompt = prompt.Replace(promptTag.Value, value);
            }
        }

        return prompt;
    }

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