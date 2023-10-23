using Rystem.OpenAi.Chat;

namespace ConversationalSearchPlatform.BackOffice.Extensions;

public static class ChatChoiceExtensions
{
    public static bool IsAnswerCompleted(this ChatChoice chunk, ILogger logger)
    {
        var completed = false;

        if (string.IsNullOrWhiteSpace(chunk.FinishReason))
        {
            return completed;
        }

        switch (chunk)
        {
            case { FinishReason: "stop" }:
                completed = true;
                break;

            case { FinishReason: "length" }:
                completed = true;
                logger.LogDebug("Stopped due to length");
                break;
        }

        return completed;
    }
}