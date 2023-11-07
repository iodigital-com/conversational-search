namespace ConversationalSearchPlatform.BackOffice.Bootstrap;

public class UnstructuredSettings
{
    public UnstructuredSettings()
    {
    }

    public UnstructuredSettings(string baseUrl)
    {
        BaseUrl = baseUrl;
    }

    public string BaseUrl { get; set; } = default!;
}