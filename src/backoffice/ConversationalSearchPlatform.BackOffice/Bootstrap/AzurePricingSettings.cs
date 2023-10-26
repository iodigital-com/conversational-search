namespace ConversationalSearchPlatform.BackOffice.Bootstrap;

public class AzurePricingSettings
{
    public AzurePricingSettings()
    {
    }

    public AzurePricingSettings(string baseUrl, string regionName)
    {
        BaseUrl = baseUrl;
        RegionName = regionName;
    }

    public string RegionName { get; set; }
    public string BaseUrl { get; set; }
}