namespace ConversationalSearchPlatform.BackOffice.Bootstrap;

public record SitemapStorageSettings
{
    public SitemapStorageSettings(string connectionString, string containerName)
    {
        ConnectionString = connectionString;
        ContainerName = containerName;
    }

    public SitemapStorageSettings()
    {
    }

    public string ConnectionString { get; set; } = default!;
    public string ContainerName { get; set; } = default!;
}