using Rystem.OpenAi.Chat;

namespace ConversationalSearchPlatform.BackOffice.Extensions;

public static class ChatResultExtensions
{
    public static string CombineAnswers(this ChatResult chatResult)
    {
        var answers = chatResult
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