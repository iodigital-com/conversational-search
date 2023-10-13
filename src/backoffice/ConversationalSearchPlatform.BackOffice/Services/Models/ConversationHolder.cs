namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public class ConversationHolder(ChatModel model, int amountOfSearchReferences)
{

    public List<(string prompt, string response)> PromptResponses { get; set; } = new List<(string prompt, string response)>();
    public ChatModel Model { get; init; } = model;
    public int AmountOfSearchReferences { get; init; } = amountOfSearchReferences;
}