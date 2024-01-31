using ConversationalSearchPlatform.BackOffice.Services.Models;
using Rystem.OpenAi.Chat;

namespace ConversationalSearchPlatform.BackOffice.Extensions;

public static class ChatBuilderExtensions
{
    public static ChatRequestBuilder AddPreviousMessages(this ChatRequestBuilder chatRequestBuilder, List<ConversationExchange> previousMessages)
    {
        foreach (var conversation in previousMessages)
        {
            chatRequestBuilder.AddUserMessage(conversation.Prompt);
            chatRequestBuilder.AddAssistantMessage(conversation.Response);
        }

        return chatRequestBuilder;
    }
}