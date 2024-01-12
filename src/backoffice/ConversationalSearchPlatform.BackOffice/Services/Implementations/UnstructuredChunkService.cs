using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class UnstructuredChunkService : IChunkService
{
    private readonly HttpClient _httpClient;

    public UnstructuredChunkService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ChunkCollection> ChunkAsync(ChunkInput chunkInput)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(chunkInput.HtmlContent));
        using var request = new HttpRequestMessage(HttpMethod.Post, "general/v0/general");
        using var content = new MultipartFormDataContent
        {
            {
                new StreamContent(stream), "files", $"content-{Guid.NewGuid()}.html"
            },
            {
                new StringContent(chunkInput.Language.ToUnstructuredChunkerLanguage()), "languages"
            },
        };

        //TODO experiment with this chunking strategy by maybe not using it
        //content.Add(new StringContent("by_title"), "chunking_strategy");

        request.Content = content;

        var responseMessage = await _httpClient.SendAsync(request);
        responseMessage.EnsureSuccessStatusCode();

        var chunks = await responseMessage.Content.ReadFromJsonAsync<List<ChunkResult>>() ?? throw new InvalidOperationException($"Cannot read {nameof(ChunkResult)}");

        return new ChunkCollection(
            chunkInput.TenantId,
            chunkInput.InternalId,
            chunkInput.Url,
            chunkInput.ReferenceType.ToString(),
            chunkInput.Language.ToString(),
            chunks
        );
    }
}