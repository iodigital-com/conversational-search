using System.Text.Json;
using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Events;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate;
using ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Queries;
using ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Schemas;
using GraphQL;
using GraphQL.Client.Abstractions;
using Rystem.OpenAi;
using Rystem.OpenAi.Embedding;
using JsonException = Newtonsoft.Json.JsonException;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class WeaviateVectorizationService : IVectorizationService
{
    private const string WeaviateHttpClientName = "Weaviate";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IGraphQLClient _graphQLClient;
    private readonly IOpenAiFactory _openAiFactory;
    private readonly ILogger<WeaviateVectorizationService> _logger;
    private readonly IWeaviateRecordCreator<ImageResult, ImageCollection, ImageWeaviateCreateRecord> _imageCreator;
    private readonly IWeaviateRecordCreator<ChunkResult, ChunkCollection, WebsitePageWeaviateCreateRecord> _websitePageCreator;
    private readonly IOpenAIUsageTelemetryService _telemetryService;

    public WeaviateVectorizationService(ILogger<WeaviateVectorizationService> logger,
        IHttpClientFactory httpClientFactory,
        IGraphQLClient graphQLClient,
        IOpenAiFactory openAiFactory,
        IWeaviateRecordCreator<ChunkResult, ChunkCollection, WebsitePageWeaviateCreateRecord> websitePageCreator,
        IWeaviateRecordCreator<ImageResult, ImageCollection, ImageWeaviateCreateRecord> imageCreator,
        IOpenAIUsageTelemetryService telemetryService)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _graphQLClient = graphQLClient;
        _openAiFactory = openAiFactory;
        _websitePageCreator = websitePageCreator;
        _imageCreator = imageCreator;
        _telemetryService = telemetryService;
    }

    public async Task<float[]> CreateVectorAsync(Guid correlationId, string tenantId, UsageType usageType, string content)
    {
        var openAiEmbedding = _openAiFactory.CreateEmbedding();
        return await GetVectorDataAsync(openAiEmbedding, correlationId, tenantId, usageType, content);
    }

    public async Task<List<Guid>> BulkCreateAsync<T>(string collectionName, Guid correlationId, string tenantId, UsageType usageType, IInsertableCollection<T> insertableCollection)
        where T : IInsertable
    {
        var openAiEmbedding = _openAiFactory.CreateEmbedding();
        var objectIds = new List<Guid>(insertableCollection.Items.Capacity);

        if (!await DoesCollectionExistAsync(collectionName))
        {
            await CreateCollectionAsync(collectionName);
        }

        foreach (var item in insertableCollection.Items)
        {
            await ProcessItemAsync(collectionName, correlationId, tenantId, usageType, insertableCollection, openAiEmbedding, item, objectIds);
        }

        return objectIds;
    }


    private async Task ProcessItemAsync<T>(
        string collectionName,
        Guid correlationId,
        string tenantId,
        UsageType usageType,
        IInsertableCollection<T> insertableCollection,
        IOpenAiEmbedding openAiEmbedding,
        T item, List<Guid> objectIds)
        where T : IInsertable
    {
        try
        {
            object weaviateCreateObject = collectionName switch
            {
                //TODO this is dirty casting
                nameof(WebsitePage) => await _websitePageCreator.CreateRecordAsync(
                    collectionName,
                    correlationId,
                    tenantId,
                    usageType,
                    (insertableCollection as ChunkCollection)!,
                    openAiEmbedding,
                    (item as ChunkResult)!),
                IndexingConstants.ImageClass => await _imageCreator.CreateRecordAsync(
                    collectionName,
                    correlationId,
                    tenantId,
                    usageType,
                    (insertableCollection as ImageCollection)!,
                    openAiEmbedding,
                    (item as ImageResult)!),
                _ => throw new NotImplementedException($"No schema creation for type. {collectionName}")
            };


            WeaviateObject weaviateObject;

            var response = await _httpClientFactory.CreateClient(WeaviateHttpClientName)
                .PostAsJsonAsync("v1/objects/", weaviateCreateObject);
            weaviateObject = await ValidateAndParse<WeaviateObject>(response) ?? throw new InvalidOperationException();

            objectIds.Add(weaviateObject.Id);
        }
        catch (Exception)
        {
            _logger.LogError("Something went wrong trying to process item");
        }
    }


    public async Task<List<R>> SearchAsync<T, R>(string key, GraphQLRequest graphQLRequest, CancellationToken cancellationToken = default)
        where T : IQueryParams
        where R : class
    {
        var response = await GraphGQLGetQueryAsync<R>(key, graphQLRequest);
        return response;
    }

    public async Task BulkDeleteAsync(string collectionName, List<Guid> idsToDelete)
    {
        foreach (var id in idsToDelete)
        {
            try
            {
                var response = await _httpClientFactory.CreateClient(WeaviateHttpClientName)
                    .DeleteAsync($"v1/objects/{collectionName}/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to delete record with id {Id} for collection {CollectionName}", id, collectionName);
                throw;
            }
        }
    }

    private async Task<List<T>> GraphGQLGetQueryAsync<T>(string key, GraphQLRequest request)
    {
        var response = await _graphQLClient.SendQueryAsync<Data<Dictionary<string, object>>>(request);

        if (response.Errors != null && response.Errors.Length != 0)
        {
            throw new InvalidOperationException(string.Join(",", response.Errors.Select(error => error.Message)));
        }

        var innerResponse = response.Data.Get[key] is JsonElement ? (JsonElement)response.Data.Get[key] : default;
        return innerResponse.Deserialize<List<T>>() ?? new List<T>();
    }

    private async Task<float[]> GetVectorDataAsync(IOpenAiEmbedding openAiEmbeddingFactory, Guid correlationId, string tenantId, UsageType usageType, string content)
    {
        var embeddingResult = await openAiEmbeddingFactory
            .Request(content)
            .WithModel(EmbeddingModelType.AdaTextEmbedding)
            .ExecuteAndCalculateCostAsync();

        _telemetryService.RegisterEmbeddingUsage(correlationId, tenantId, embeddingResult.Result.Usage!, usageType);

        return (embeddingResult.Result.Data ?? new List<EmbeddingData>())
               .Select(data => data.Embedding)
               .First() ??
               Array.Empty<float>();
    }

    private async Task<bool> DoesCollectionExistAsync(string collectionName)
    {
        var response = await _httpClientFactory.CreateClient(WeaviateHttpClientName)
            .GetAsync($"v1/schema/{collectionName}");
        return response.IsSuccessStatusCode;
    }

    private async Task CreateCollectionAsync(string collectionName)
    {
        //TODO very hardcoded, we want some kind of collection name to schema registry
        var schema = collectionName switch
        {
            nameof(WebsitePage) => Schemas.PageChunkSchema(collectionName),
            IndexingConstants.ImageClass => Schemas.ImageChunkSchema(collectionName),
            _ => null
        };

        var response = await _httpClientFactory.CreateClient(WeaviateHttpClientName)
            .PostAsJsonAsync($"v1/schema/", schema);
        response.EnsureSuccessStatusCode();
    }


    private async Task<T?> ValidateAndParse<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<T>() ?? throw new InvalidOperationException();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Something went wrong with the HTTP call");
            throw;
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex, "Expected content type was something different");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON");
            throw;
        }
    }
}