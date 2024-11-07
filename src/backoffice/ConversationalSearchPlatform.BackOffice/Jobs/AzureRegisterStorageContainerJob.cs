using ConversationalSearchPlatform.BackOffice.Services;

namespace ConversationalSearchPlatform.BackOffice.Jobs;

/// <summary>
/// Creates the storage account at startup. Only useful for development purposes where the storage container has not been initialized for Azurite.
/// </summary>
public class AzureRegisterStorageContainerJob(IAzureBlobStorage blobStorage) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
        //await blobStorage.CreateStorageContainerIfNotExistsAsync();
}