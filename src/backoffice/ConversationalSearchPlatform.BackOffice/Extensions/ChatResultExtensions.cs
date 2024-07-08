using OpenAI.Chat;

namespace ConversationalSearchPlatform.BackOffice.Extensions;

public static class ChatResultExtensions
{
    public static ChatMessage GetFirstAnswer(this ChatCompletion chatResult)
    {
        var answer = chatResult
                          .Content
                          .FirstOrDefault()?
                          .Text;

        if (answer == null)
        {
            return new AssistantChatMessage(string.Empty);
        }

        return new AssistantChatMessage(answer);
    }

    /*public static string CombineStreamAnswer(this StreamingChatResult chatResult)
    {
        var chatMessage = chatResult
            .Composed
            .Choices!
            .Where(choice => choice.Message != null && choice.Message!.Content != null)
            .Select(choice => choice.Message)
            .First();

        // For some reason the internal StringBuilder content does not get exposed onto the Content property, so we force it to toString();
        chatMessage.InvokeMethod("BuildContent");
        return chatMessage!.Content ?? "";
    }*/
}