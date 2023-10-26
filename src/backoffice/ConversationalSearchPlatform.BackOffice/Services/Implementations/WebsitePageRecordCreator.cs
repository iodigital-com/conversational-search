using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate;
using Rystem.OpenAi;
using Rystem.OpenAi.Embedding;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public interface IWeaviateRecordCreator<TInsertable, TCollection, TWeaviateCreateRecord>
    where TInsertable : IInsertable
    where TCollection : IInsertableCollection<TInsertable>
    where TWeaviateCreateRecord : IWeaviateCreateRecord

{
    Task<WeaviateCreateObject<TWeaviateCreateRecord>> CreateRecordAsync(
        string collectionName,
        Guid correlationId,
        string tenantId,
        UsageType usageType,
        TCollection collection,
        IOpenAiEmbedding openAiEmbedding,
        TInsertable item);
}

public abstract class BaseWeaviateRecordCreator(IOpenAIUsageTelemetryService telemetryService)
{

    protected async Task<float[]> GetVectorDataAsync(IOpenAiEmbedding openAiEmbeddingFactory, Guid correlationId, string tenantId, UsageType usageType, string content)
    {
        var embeddingResult = await openAiEmbeddingFactory
            .Request(content)
            .WithModel(EmbeddingModelType.AdaTextEmbedding)
            .ExecuteAndCalculateCostAsync();

        telemetryService.RegisterEmbeddingUsage(correlationId, tenantId, embeddingResult.Result.Usage!, usageType);

        return (embeddingResult.Result.Data ?? new List<EmbeddingData>())
               .Select(data => data.Embedding)
               .First() ??
               Array.Empty<float>();
    }
}

public sealed class WebsitePageRecordCreator<TInsertable, TCollection, TWeaviateCreateRecord>(IOpenAIUsageTelemetryService telemetryService)
    : BaseWeaviateRecordCreator(telemetryService), IWeaviateRecordCreator<ChunkResult, ChunkCollection, WebsitePageWeaviateCreateRecord>
{
    public async Task<WeaviateCreateObject<WebsitePageWeaviateCreateRecord>> CreateRecordAsync(
        string collectionName,
        Guid correlationId,
        string tenantId,
        UsageType usageType,
        ChunkCollection collection,
        IOpenAiEmbedding openAiEmbedding,
        ChunkResult item)
    {
        var vectorData = await this.GetVectorDataAsync(openAiEmbedding, correlationId, tenantId, usageType, item.Text);

        var record = new WebsitePageWeaviateCreateRecord(collection.TenantId,
            collection.InternalId,
            item.Text,
            collection.Url,
            collection.Language,
            collection.ReferenceType);

        var weaviateCreateObject = new WeaviateCreateObject<WebsitePageWeaviateCreateRecord>(
            collectionName,
            vectorData,
            record
        );

        return weaviateCreateObject;
    }
}

public sealed class ImageRecordCreator<TInsertable, TCollection, TWeaviateCreateRecord>(IOpenAIUsageTelemetryService telemetryService)
    : BaseWeaviateRecordCreator(telemetryService), IWeaviateRecordCreator<ImageResult, ImageCollection, ImageWeaviateCreateRecord>
{

    public Task<WeaviateCreateObject<ImageWeaviateCreateRecord>> CreateRecordAsync(
        string collectionName,
        Guid correlationId,
        string tenantId,
        UsageType usageType,
        ImageCollection collection,
        IOpenAiEmbedding openAiEmbedding,
        ImageResult item)
    {
        var weaviateCreateObject = new WeaviateCreateObject<ImageWeaviateCreateRecord>(
            collectionName,
            null,
            new ImageWeaviateCreateRecord(
                item!.FileName,
                item.InternalId,
                item.AltDescription,
                item.NearByText,
                item.Url,
                item.ImageBlob
            )
        );
        return Task.FromResult(weaviateCreateObject);
    }
}