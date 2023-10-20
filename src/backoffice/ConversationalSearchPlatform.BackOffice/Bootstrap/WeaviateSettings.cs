namespace ConversationalSearchPlatform.BackOffice.Bootstrap;

public record WeaviateSettings
{
    public WeaviateSettings()
    {
        
    }
    public WeaviateSettings(string? ApiKey, string BaseUrl)
    {
        this.ApiKey = ApiKey;
        this.BaseUrl = BaseUrl;
    }

    public string? ApiKey { get; init; }
    public string BaseUrl { get; init; }

    public void Deconstruct(out string? ApiKey, out string BaseUrl)
    {
        ApiKey = this.ApiKey;
        BaseUrl = this.BaseUrl;
    }
}