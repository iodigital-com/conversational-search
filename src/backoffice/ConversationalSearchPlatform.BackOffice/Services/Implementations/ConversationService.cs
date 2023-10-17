using System.Text;
using ConversationalSearchPlatform.BackOffice.Exceptions;
using ConversationalSearchPlatform.BackOffice.Resources;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Microsoft.Extensions.Caching.Memory;
using Rystem.OpenAi;
using Rystem.OpenAi.Chat;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class ConversationService : IConversationService
{
    private readonly IMemoryCache _conversationsCache;
    private readonly IOpenAiFactory _openAiFactory;
    private readonly IMultiTenantStore<ApplicationTenantInfo> _tenantStore;

    private readonly MemoryCacheEntryOptions _defaultMemoryCacheEntryOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(2)
    };

    public ConversationService(IMemoryCache conversationsCache, IOpenAiFactory openAiFactory, IMultiTenantStore<ApplicationTenantInfo> tenantStore)
    {
        _conversationsCache = conversationsCache;
        _openAiFactory = openAiFactory;
        _tenantStore = tenantStore;
    }

    public Task<ConversationId> StartConversationAsync(StartConversation startConversation)
    {
        var conversationId = Guid.NewGuid();
        var cacheKey = GetCacheKey(conversationId);
        _conversationsCache.Set(cacheKey,
            new ConversationHolder(startConversation.Model, startConversation.AmountOfSearchReferences),
            _defaultMemoryCacheEntryOptions);

        return Task.FromResult(new ConversationId(conversationId));
    }

    public async Task<ConversationReferencedResult> ConverseAsync(HoldConversation holdConversation)
    {
        var promptText = holdConversation.Prompt.Trim();

        var cacheKey = GetCacheKey(holdConversation.ConversationId);

        if (!_conversationsCache.TryGetValue(cacheKey, out ConversationHolder? conversationHistory))
        {
            ThrowHelper.ThrowConversationNotFoundException(holdConversation.ConversationId);
        }

        //TODO this implementation needs to be completed
        var references = await GetReferences(promptText);
        var basePrompt = (await GetEmbeddedResourceText(ResourceConstants.BasePromptFile)).Trim();

        var tenantPrompt = await GetTenantPromptAsync(holdConversation.TenantId);
        basePrompt = basePrompt.Replace("{{TenantPrompt}}", tenantPrompt);

        var sources = GetKnowledgeBase(references);
        basePrompt = basePrompt.Replace("{{Sources}}", sources);

        var chatBuilder = _openAiFactory.CreateChat()
            .RequestWithSystemMessage(basePrompt);

        foreach (var conversation in conversationHistory!.PromptResponses)
        {
            chatBuilder.AddUserMessage(conversation.prompt);
            chatBuilder.AddAssistantMessage(conversation.prompt);
        }

        chatBuilder.AddUserMessage(holdConversation.Prompt);
        chatBuilder
            .WithModel((ChatModelType)conversationHistory.Model)
            .WithTemperature(1);

        var chatResult = await chatBuilder
            .ExecuteAndCalculateCostAsync();

        var completeAnswer = ParseAnswer(chatResult);

        SaveConversationHistory(cacheKey, conversationHistory, promptText, completeAnswer);

        var index = 0;

        var validReferences =
            references
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

    private void SaveConversationHistory(string cacheKey, ConversationHolder conversationHistory, string promptText, string completeAnswer)
    {
        conversationHistory.PromptResponses.Add((promptText, completeAnswer));
        _conversationsCache.Set(cacheKey, conversationHistory);
    }

    private async Task<string> GetTenantPromptAsync(string tenantId)
    {
        var tenant = await _tenantStore.TryGetAsync(tenantId);

        if (tenant == null)
        {
            ThrowHelper.ThrowTenantNotFoundException(tenantId);
        }

        return tenant.GetBasePromptOrDefault();
    }

    private static string GetKnowledgeBase(List<SearchReference> references)
    {
        var knowledgeBaseBuilder = new StringBuilder();

        foreach (var reference in references)
        {
            knowledgeBaseBuilder.AppendLine($"{reference.Source} -> {reference.Content.ReplaceLineEndings(" ")}");
        }

        return knowledgeBaseBuilder.ToString();
    }

    //TODO call weaviate here
    private static Task<List<SearchReference>> GetReferences(string prompt)
    {
        return Task.FromResult(new List<SearchReference>());
    }

    private async Task<string> GetEmbeddedResourceText(string resourceName)
    {
        var resourceContents = string.Empty;
        var fullResourceName = $"ConversationalSearchPlatform.BackOffice.Resources.{resourceName}";
        var assembly = this.GetType().Assembly;

        using (var stream = assembly.GetManifestResourceStream(fullResourceName))
            if (stream != null)
            {
                using (var reader = new StreamReader(stream))
                {
                    resourceContents = (await reader.ReadToEndAsync()).Trim();
                }
            }

        return resourceContents;
    }

    private static string GetCacheKey(Guid conversationId) =>
        $"conversation_{conversationId}";
}