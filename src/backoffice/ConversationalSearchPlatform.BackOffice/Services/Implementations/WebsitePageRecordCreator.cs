using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate;
using OpenAI;
using OpenAI.Embeddings;

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
        string title,
        UsageType usageType,
        TCollection collection,
        EmbeddingClient embeddingClient,
        TInsertable item);
}

public abstract class BaseWeaviateRecordCreator(IOpenAIUsageTelemetryService telemetryService)
{

    protected async Task<float[]> GetVectorDataAsync(EmbeddingClient embeddingClient, Guid correlationId, string tenantId, UsageType usageType, string content)
    {
        OpenAIEmbedding embeddingResult = await embeddingClient
            .GenerateEmbeddingAsync(content);

        //telemetryService.RegisterEmbeddingUsage(correlationId, tenantId, embeddingResult.Value, usageType);

        return embeddingResult.ToFloats().ToArray();
    }
}

public sealed class WebsitePageRecordCreator<TInsertable, TCollection, TWeaviateCreateRecord>(IOpenAIUsageTelemetryService telemetryService)
    : BaseWeaviateRecordCreator(telemetryService), IWeaviateRecordCreator<ChunkResult, ChunkCollection, WebsitePageWeaviateCreateRecord>
{
    public async Task<WeaviateCreateObject<WebsitePageWeaviateCreateRecord>> CreateRecordAsync(
        string collectionName,
        Guid correlationId,
        string tenantId,
        string title,
        UsageType usageType,
        ChunkCollection collection,
        EmbeddingClient embeddingClient,
        ChunkResult item)
    {
        var vectorData = await this.GetVectorDataAsync(embeddingClient, correlationId, tenantId, usageType, item.Text);

        var record = new WebsitePageWeaviateCreateRecord(collection.TenantId,
            collection.InternalId,
            title,
            item.Text,
            collection.Url,
            collection.Language,
            collection.ReferenceType,
            item.ArticleNumber,
            item.Packaging);

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
        string title,
        UsageType usageType,
        ImageCollection collection,
        EmbeddingClient embeddingClient,
        ImageResult item)
    {
        var record = new ImageWeaviateCreateRecord(
            item.FileName,
            item.InternalId,
            item.AltDescription,
            item.NearByText,
            item.Url,
            title,
            item.ImageBlob
        );

        var weaviateCreateObject = new WeaviateCreateObject<ImageWeaviateCreateRecord>(
            collectionName,
            null,
            record
        );
        return Task.FromResult(weaviateCreateObject);
    }
}