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

    public string RegionName { get; set; } = default!;
    public string BaseUrl { get; set; } = default!;
}