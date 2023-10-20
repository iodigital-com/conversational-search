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
    Task<WeaviateCreateObject<TWeaviateCreateRecord>> CreateRecordAsync(string collectionName, TCollection collection, IOpenAiEmbedding openAiEmbedding,
        TInsertable item);
}

public abstract class BaseWeaviateRecordCreator
{
    protected static async Task<float[]> GetVectorDataAsync(IOpenAiEmbedding openAiEmbeddingFactory, string content)
    {
        var embeddingResult = await openAiEmbeddingFactory
            .Request(content)
            .WithModel(EmbeddingModelType.AdaTextEmbedding)
            .ExecuteAsync();

        return (embeddingResult.Data ?? new List<EmbeddingData>())
               .Select(data => data.Embedding)
               .First() ??
               Array.Empty<float>();
    }
}

public sealed class WebsitePageRecordCreator<TInsertable, TCollection, TWeaviateCreateRecord> : BaseWeaviateRecordCreator,
    IWeaviateRecordCreator<ChunkResult, ChunkCollection, WebsitePageWeaviateCreateRecord>
{
    public async Task<WeaviateCreateObject<WebsitePageWeaviateCreateRecord>> CreateRecordAsync(
        string collectionName,
        ChunkCollection collection,
        IOpenAiEmbedding openAiEmbedding,
        ChunkResult item)
    {
        var vectorData = await GetVectorDataAsync(openAiEmbedding, item.Text);

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

public sealed class ImageRecordCreator<TInsertable, TCollection, TWeaviateCreateRecord> : BaseWeaviateRecordCreator,
    IWeaviateRecordCreator<ImageResult, ImageCollection, ImageWeaviateCreateRecord>
{
    public Task<WeaviateCreateObject<ImageWeaviateCreateRecord>> CreateRecordAsync(
        string collectionName,
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