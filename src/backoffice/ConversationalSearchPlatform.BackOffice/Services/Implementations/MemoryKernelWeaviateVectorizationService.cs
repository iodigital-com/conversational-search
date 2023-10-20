// using System.Runtime.CompilerServices;
// using System.Text.Json;
// using ConversationalSearchPlatform.BackOffice.Bootstrap;
// using ConversationalSearchPlatform.BackOffice.Data.Entities;
// using ConversationalSearchPlatform.BackOffice.Services.Models;
// using GraphQL.Client.Abstractions;
// using Microsoft.Extensions.Options;
// using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
// using Microsoft.SemanticKernel.Connectors.Memory.Weaviate;
// using Microsoft.SemanticKernel.Memory;
// using Microsoft.SemanticKernel.Plugins.Memory;
//
// namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;
//
// public class MemoryKernelWeaviateVectorizationService : IVectorizationService
// {
//     private readonly OpenAISettings _openAiSettings;
//     private readonly ILoggerFactory _loggerFactory;
//     private readonly WeaviateSettings _weaviateSettings;
//
//
//     public MemoryKernelWeaviateVectorizationService(
//         IOptions<OpenAISettings> openAiSettings,
//         IOptions<WeaviateSettings> weaviateSettings,
//         ILoggerFactory loggerFactory
//     )
//     {
//         _loggerFactory = loggerFactory;
//         _openAiSettings = openAiSettings.Value;
//         _weaviateSettings = weaviateSettings.Value;
//     }
//
//     public async Task BulkCreateAsync(string collectionName, ChunkCollection chunkCollection)
//     {
//         var memory = InitializeMemory();
//         var memoryMetaData = JsonSerializer.Serialize(new MemoryMetaData(chunkCollection.Url, chunkCollection.ReferenceType, chunkCollection.Language));
//
//         foreach (var chunk in chunkCollection.Chunks)
//         {
//             await memory.SaveInformationAsync(collectionName, chunk.Text, Guid.NewGuid().ToString(), null, memoryMetaData);
//         }
//     }
//
//     public async IAsyncEnumerable<MemoryQueryResult> SearchAsync(
//         string collectionName,
//         string content,
//         int limit,
//         [EnumeratorCancellation] CancellationToken cancellationToken = default)
//     {
//         var memory = InitializeMemory();
//
//         await foreach (var entry in memory.SearchAsync(collectionName, content, limit, cancellationToken: cancellationToken))
//         {
//             yield return entry;
//         }
//     }
//
//     private ISemanticTextMemory InitializeMemory()
//     {
//         var apiKey = string.IsNullOrWhiteSpace(_weaviateSettings.ApiKey) ? null : _weaviateSettings.ApiKey;
//
//         return new MemoryBuilder()
//             .WithLoggerFactory(_loggerFactory)
//             .WithAzureTextEmbeddingGenerationService("text-embedding-ada-002-io-gpt", "https://" + _openAiSettings.ResourceName + ".openai.azure.com/", _openAiSettings.ApiKey)
//             .WithMemoryStore(new WeaviateMemoryStore(_weaviateSettings.BaseUrl, apiKey))
//             .Build();
//     }
// }