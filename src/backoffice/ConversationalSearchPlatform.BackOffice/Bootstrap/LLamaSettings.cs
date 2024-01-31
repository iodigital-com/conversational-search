namespace ConversationalSearchPlatform.BackOffice.Bootstrap;

public record LLamaSettings
{
    public LLamaSettings()
    {
    }

    public LLamaSettings(string endpoint, string apiToken)
    {
        Endpoint = endpoint;
        ApiToken = apiToken;
    }

    public string Endpoint { get; init; }
    public string ApiToken { get; init; }

    public void Deconstruct(out string Endpoint, out string ApiToken)
    {
        Endpoint = this.Endpoint;
        ApiToken = this.ApiToken;
    }
}