using System.Collections.Specialized;
using System.Text;
using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Exceptions;
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

public class ConversationService : IConversationService
{
    private readonly IMemoryCache _conversationsCache;
    private readonly IOpenAiFactory _openAiFactory;
    private readonly IMultiTenantStore<ApplicationTenantInfo> _tenantStore;
    private readonly IVectorizationService _vectorizationService;


    private readonly MemoryCacheEntryOptions _defaultMemoryCacheEntryOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(2)
    };

    public ConversationService(
        IMemoryCache conversationsCache,
        IOpenAiFactory openAiFactory,
        IMultiTenantStore<ApplicationTenantInfo> tenantStore,
        IVectorizationService vectorizationService)
    {
        _conversationsCache = conversationsCache;
        _openAiFactory = openAiFactory;
        _tenantStore = tenantStore;
        _vectorizationService = vectorizationService;
    }

    public Task<ConversationId> StartConversationAsync(StartConversation startConversation, CancellationToken cancellationToken)
    {
        var conversationId = Guid.NewGuid();
        var cacheKey = GetCacheKey(conversationId);
        _conversationsCache.Set(cacheKey,
            new ConversationHolder(startConversation.Model, startConversation.AmountOfSearchReferences),
            _defaultMemoryCacheEntryOptions);

        return Task.FromResult(new ConversationId(conversationId));
    }

    public async Task<ConversationReferencedResult> ConverseAsync(HoldConversation holdConversation, CancellationToken cancellationToken)
    {
        holdConversation.Prompt = holdConversation.Prompt.Trim();

        var cacheKey = GetCacheKey(holdConversation.ConversationId);

        var conversationHistory = GetConversationHistory(holdConversation, cacheKey);

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

        var references = textReferences.Select((textRef, i) => new SortedSearchReference
            {
                TextSearchReference = textRef,
                Index = i + 1
            })
            .ToList();

        basePrompt = basePrompt.Replace("{{TextSources}}", FlattenTextReferences(references));
        basePrompt = basePrompt.Replace("{{ImageSources}}", FlattenImageReferences(references, imageSearchReferences));

        var chatBuilder = _openAiFactory.CreateChat()
            .RequestWithSystemMessage(basePrompt);

        foreach (var conversation in conversationHistory.PromptResponses)
        {
            chatBuilder.AddUserMessage(conversation.prompt);
            chatBuilder.AddAssistantMessage(conversation.prompt);
        }

        chatBuilder.AddUserMessage(holdConversation.Prompt);
        chatBuilder
            .WithModel((ChatModelType)conversationHistory.Model)
            .WithTemperature(1);

        var chatResult = await chatBuilder
            .ExecuteAndCalculateCostAsync(cancellationToken: cancellationToken);

        var completeAnswer = ParseAnswer(chatResult);

        AppendToConversationHistory(cacheKey, conversationHistory, holdConversation.Prompt, completeAnswer);

        var index = 0;

        var validReferences =
            textReferences
                .Where(document => completeAnswer.Contains(document.Source))
                .Select(validReference => new ConversationReference(++index, validReference.Source, validReference.Type))
                .ToList();

        return new ConversationReferencedResult(
            new ConversationResult(
                holdConversation.ConversationId,
                completeAnswer,
                holdConversation.Language
            ),
            validReferences);
    }

    private ConversationHolder GetConversationHistory(HoldConversation holdConversation, string cacheKey)
    {
        if (!_conversationsCache.TryGetValue(cacheKey, out ConversationHolder? conversationHistory))
        {
            ThrowHelper.ThrowConversationNotFoundException(holdConversation.ConversationId);
        }

        return conversationHistory!;
    }

    private static string ParseAnswer(CostResult<ChatResult> chatResult)
    {
        var answers = chatResult
                          .Result
                          .Choices?
                          .Select(choice => choice.Message)
                          .Where(message => message != null)
                          .Select(message => message!)
                          .Where(msg => msg.Role == ChatRole.Assistant)
                          .Select(message => message.Content)
                          .Where(content => content != null) ??
                      Enumerable.Empty<string>();

        return string.Join(Environment.NewLine, answers)
            .ReplaceLineEndings();
    }

    private void AppendToConversationHistory(string cacheKey, ConversationHolder conversationHistory, string promptText, string completeAnswer)
    {
        conversationHistory.PromptResponses.Add((promptText, completeAnswer));
        _conversationsCache.Set(cacheKey, conversationHistory);
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
        ConversationHolder conversationHistory,
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
        ConversationHolder conversationHistory,
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

        return search.Select(result =>
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

    private static string GetCacheKey(Guid conversationId) =>
        $"conversation_{conversationId}";
}