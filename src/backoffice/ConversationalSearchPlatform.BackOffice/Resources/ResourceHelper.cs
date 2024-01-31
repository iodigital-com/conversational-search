namespace ConversationalSearchPlatform.BackOffice.Resources;

public static class ResourceHelper
{
    public const string BasePromptFile = "BasePrompt.txt";
    public const string KeywordsPromptFile = "KeywordsPrompt.txt";

    public static async Task<string> GetEmbeddedResourceTextAsync(string resourceName)
    {
        var resourceContents = string.Empty;
        var fullResourceName = $"ConversationalSearchPlatform.BackOffice.Resources.{resourceName}";
        var assembly = typeof(ResourceHelper).Assembly;

        using var stream = assembly.GetManifestResourceStream(fullResourceName);

        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            resourceContents = (await reader.ReadToEndAsync()).Trim();
        }

        return resourceContents;
    }
}