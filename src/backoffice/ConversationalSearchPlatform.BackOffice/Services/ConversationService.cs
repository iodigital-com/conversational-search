using System.Text;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using ConversationalSearchPlatform.BackOffice.Exceptions;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Microsoft.Extensions.Caching.Memory;
using Rystem.OpenAi;
using Rystem.OpenAi.Chat;

namespace ConversationalSearchPlatform.BackOffice.Services;

public class ConversationService : IConversationService
{
    private readonly IMemoryCache _conversationsCache;
    private readonly IOpenAiFactory _openAiFactory;
    private readonly IMultiTenantStore<ApplicationTenantInfo> _tenantStore;

    private readonly MemoryCacheEntryOptions _defaultMemoryCacheEntryOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(2)
    };

    public ConversationService(IMemoryCache conversationsCache, IOpenAiFactory openAiFactory)
    {
        _conversationsCache = conversationsCache;
        _openAiFactory = openAiFactory;
    }

    public Task<ConversationId> StartConversationAsync(string tenantId, ChatModel model, int amountOfSearchReferences)
    {
        var conversationId = Guid.NewGuid();
        var cacheKey = GetCacheKey(conversationId);
        _conversationsCache.Set(cacheKey, new ConversationHolder(model, amountOfSearchReferences), _defaultMemoryCacheEntryOptions);

        return Task.FromResult(new ConversationId(conversationId));
    }

    public async Task<ConversationReferencedResult> ConverseAsync(Guid conversationId, string tenantId, string prompt)
    {
        var promptText = prompt.Trim();

        var cacheKey = GetCacheKey(conversationId);

        if (!_conversationsCache.TryGetValue(cacheKey, out ConversationHolder? conversationHistory))
        {
            ThrowHelper.ThrowConversationNotFoundException(conversationId);
        }

        //TODO this implementation needs to
        var references = await GetReferences(promptText);
        var basePrompt = (await GetEmbeddedResourceText("BasePrompt.txt")).Trim();

        var tenantPrompt = await GetTenantPromptAsync(tenantId);
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

        chatBuilder.AddUserMessage(prompt);
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
                .Where(document => completeAnswer.Contains(document.Document.Source))
                .Select(validReference => new ConversationReference(++index, validReference.Document.Source))
                .ToList();

        return new ConversationReferencedResult(new ConversationResponse(conversationId, completeAnswer), validReferences);
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

    private static string GetKnowledgeBase(List<SearchResult<Document>> references)
    {
        var knowledgeBaseBuilder = new StringBuilder();

        foreach (var reference in references)
        {
            knowledgeBaseBuilder.AppendLine($"{reference.Document.Source} -> {reference.Document.DocumentText.ReplaceLineEndings(" ")}");
        }

        return knowledgeBaseBuilder.ToString();
    }

    private static async Task<List<SearchResult<Document>>> GetReferences(string prompt)
    {
        // var searchClient = new SearchClient(new Uri("someUrl"),
        //     "someIndex",
        //     new AzureKeyCredential("someApiKey"));
        //
        // var searchResults = await searchClient.SearchAsync<Document>(prompt);
        // var documents = searchResults.Value;
        //
        // return documents.GetResults().Take(7).ToList();
        return new List<SearchResult<Document>>();
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

    private static string GetCacheKey(Guid conversationId)
    {
        var cacheKey = "conversation_" + conversationId;
        return cacheKey;
    }
}