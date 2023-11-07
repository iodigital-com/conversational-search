namespace ConversationalSearchPlatform.BackOffice.Bootstrap;

public record OpenAISettings
{
    public OpenAISettings()
    {
    }

    public OpenAISettings(bool UseAzure, string ApiKey, string ResourceName, string VersionForChat)
    {
        this.UseAzure = UseAzure;
        this.ApiKey = ApiKey;
        this.ResourceName = ResourceName;
        this.VersionForChat = VersionForChat;
    }

    public bool UseAzure { get; init; }
    public string ApiKey { get; init; } = default!;
    public string ResourceName { get; init; } = default!;
    public string VersionForChat { get; init; } = default!;

    public void Deconstruct(out bool UseAzure, out string ApiKey, out string ResourceName, out string VersionForChat)
    {
        UseAzure = this.UseAzure;
        ApiKey = this.ApiKey;
        ResourceName = this.ResourceName;
        VersionForChat = this.VersionForChat;
    }
}