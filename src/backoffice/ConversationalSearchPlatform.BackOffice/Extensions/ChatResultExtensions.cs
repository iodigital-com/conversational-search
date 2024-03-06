using Rystem.OpenAi.Chat;

namespace ConversationalSearchPlatform.BackOffice.Extensions;

public static class ChatResultExtensions
{
    public static ChatMessage GetFirstAnswer(this ChatResult chatResult)
    {
        var answer = chatResult
                          .Choices?
                          .FirstOrDefault()?
                          .Message;

        if (answer == null)
        {
            answer = new ChatMessage()
            {
                Role = ChatRole.Assistant,
                Content = string.Empty,
            };
        }

        return answer;
    }

    public static string CombineStreamAnswer(this StreamingChatResult chatResult)
    {
        var chatMessage = chatResult
            .Composed
            .Choices!
            .Where(choice => choice.Message != null && choice.Message!.Content != null)
            .Select(choice => choice.Message)
            .First();

        // For some reason the internal StringBuilder content does not get exposed onto the Content property, so were force it to toString();
        chatMessage.InvokeMethod("BuildContent");
        return chatMessage!.Content ?? "";
    }
}