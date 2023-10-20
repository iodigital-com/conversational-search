namespace ConversationalSearchPlatform.BackOffice.Bootstrap;

public record PuppeteerSettings
{
    public PuppeteerSettings()
    {
    }

    public PuppeteerSettings(string BaseUrl)
    {
        this.BaseUrl = BaseUrl;
    }

    public string BaseUrl { get; set; }

}