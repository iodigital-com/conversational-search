using ConversationalSearchPlatform.BackOffice.Services.Models;
using OpenAI.Chat;

namespace ConversationalSearchPlatform.BackOffice.Extensions;

public static class ChatBuilderExtensions
{
    public static List<ChatMessage> AddPreviousMessages(this List<ChatMessage> chatRequestBuilder, List<ConversationExchange> previousMessages)
    {
        foreach (var conversation in previousMessages)
        {
            chatRequestBuilder.Add(conversation.Prompt);
            chatRequestBuilder.Add(conversation.Response);
        }

        return chatRequestBuilder;
    }
}