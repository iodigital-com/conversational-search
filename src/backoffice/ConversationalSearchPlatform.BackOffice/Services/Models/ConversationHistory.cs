using Microsoft.Extensions.Caching.Memory;

namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public class ConversationHistory(ChatModel model, int amountOfSearchReferences)
{

    public List<(string prompt, string response)> PromptResponses { get; set; } = new List<(string prompt, string response)>();
    public List<string> StreamingResponseChunks { get; set; } = new();


    public bool IsStreaming { get; set; }
    public ChatModel Model { get; init; } = model;
    public int AmountOfSearchReferences { get; init; } = amountOfSearchReferences;
    public string GetAllStreamingResponseChunksMerged() => string.Join(null, StreamingResponseChunks);

    public void AppendToConversation(string prompt, string answer)
    {
        PromptResponses.Add((prompt, answer));
    }
}

public static class ConversationHistoryExtensions
{
    public static void SaveConversationHistory(this ConversationHistory conversationHistory, IMemoryCache conversationsCache, string cacheKey) => conversationsCache.Set(cacheKey, conversationHistory);
}