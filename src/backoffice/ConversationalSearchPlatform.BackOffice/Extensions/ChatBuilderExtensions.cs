using Rystem.OpenAi.Chat;

namespace ConversationalSearchPlatform.BackOffice.Extensions;

public static class ChatBuilderExtensions
{
    public static ChatRequestBuilder AddPreviousMessages(this ChatRequestBuilder chatRequestBuilder, List<(string prompt, string response)> previousMessages)
    {
        foreach (var conversation in previousMessages)
        {
            chatRequestBuilder.AddUserMessage(conversation.prompt);
            chatRequestBuilder.AddAssistantMessage(conversation.response);
        }

        return chatRequestBuilder;
    }
}