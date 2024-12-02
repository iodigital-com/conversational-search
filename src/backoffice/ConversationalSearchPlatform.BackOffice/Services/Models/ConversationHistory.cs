using ConversationalSearchPlatform.BackOffice.Services.Models.ConversationDebug;
using Microsoft.Extensions.Caching.Memory;
using OpenAI.Chat;

namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public class ConversationHistory(ChatModel model, int amountOfSearchReferences)
{

    public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    private List<ChatMessage> _messagesToRemove { get; set; } = new List<ChatMessage>();

    public int LastReferenceIndex { get; set; } = 0;

    public List<string> StreamingResponseChunks { get; set; } = new();

    public int AnsweredMessages { get; private set; } = 0;

    public bool IsStreaming { get; set; }
    public bool IsLastChunk { get; set; } = false;

    public ChatModel Model { get; init; } = model;
    public int AmountOfSearchReferences { get; init; } = amountOfSearchReferences;
    public bool DebugEnabled { get; set; }

    public bool HasEnded { get; set; } = false;

    public DebugInformation? DebugInformation { get; set; }

    public string GetAllStreamingResponseChunksMerged() => string.Join(null, StreamingResponseChunks);

    public void AppendToConversation(ChatMessage message, bool isRemovable = false, bool doClean = false)
    {
        Messages.Add(message);

        if (isRemovable)
        {
            _messagesToRemove.Add(message);
        }

        if (doClean)
        {
            Messages.RemoveAll(_messagesToRemove.Contains);

            _messagesToRemove.Clear();
        }
    }


}

public static class ConversationHistoryExtensions
{
    public static void SaveConversationHistory(this ConversationHistory conversationHistory, IMemoryCache conversationsCache, string cacheKey) =>
        conversationsCache.Set(cacheKey, conversationHistory);


    public static void InitializeDebugInformation(this ConversationHistory conversationHistory)
    {
        conversationHistory.DebugInformation ??= new DebugInformation(new List<DebugRecord>(), 0, 0);

        conversationHistory.DebugInformation?.DebugRecords.Add(new DebugRecord
        {
            ExecutedAt = DateTimeOffset.UtcNow,
        });
        conversationHistory.DebugInformation!.CurrentDebugRecordIndex = conversationHistory.DebugInformation.DebugRecords.Count - 1;
    }

    public static void AppendPreRequestDebugInformation(
        this ConversationHistory conversationHistory,
        List<ChatMessage> chatBuilder,
        List<TextSearchReference> textReferences,
        List<ImageSearchReference> imageReferences,
        PromptBuilder promptHolder)
    {
        var debugRecord = conversationHistory.DebugInformation?.DebugRecords.ElementAt(conversationHistory.DebugInformation.CurrentDebugRecordIndex)!;

        debugRecord.FullPrompt = string.Join(Environment.NewLine, chatBuilder.Select(message => message.Content[0].Text));
        debugRecord.References = new References
        {
            Text = textReferences.Select(reference => new TextDebugInfo(reference.InternalId, false, reference.Source, reference.Content)).ToList(),
            Image = imageReferences.Select(reference => new ImageDebugInfo(reference.InternalId, false, reference.Source, reference.AltDescription)).ToList()
        };
        debugRecord.ReplacedContextVariables = promptHolder.GetReplacedPromptTags().ToDictionary(tag => tag.key, tag => tag.value);
    }

    public static void AppendPostRequestTextReferenceDebugInformation(
        this ConversationHistory conversationHistory,
        List<ConversationReference> validReferences)
    {
        var debugRecord = conversationHistory.DebugInformation!.DebugRecords.ElementAt(conversationHistory.DebugInformation.CurrentDebugRecordIndex);

        foreach (var reference in validReferences)
        {
            var matchingReference = debugRecord.References.Text.FirstOrDefault(info => info.Source == reference.Url);

            if (matchingReference != null)
            {
                matchingReference.UsedInAnswer = true;
            }
        }
    }

    public static void AppendPostRequestImageReferenceDebugInformation(
        this ConversationHistory conversationHistory,
        List<ImageSearchReference> imageSearchReferences,
        string mergedAnswer)
    {
        var debugRecord = conversationHistory.DebugInformation!.DebugRecords.ElementAt(conversationHistory.DebugInformation.CurrentDebugRecordIndex);

        foreach (var image in imageSearchReferences)
        {
            var matchingReference = debugRecord.References.Image.FirstOrDefault(info => info.Source == image.Source);

            if (matchingReference != null)
            {
                var contains = mergedAnswer.Contains(image.Source);

                if (contains)
                {
                    matchingReference.UsedInAnswer = true;
                }
            }
        }
    }
}