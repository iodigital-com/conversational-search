using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class UnstructuredChunkService : IChunkService
{
    private readonly HttpClient _httpClient;

    public UnstructuredChunkService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ChunkResult> ChunkAsync(ChunkInput chunkInput)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(chunkInput.HtmlContent));
        using var request = new HttpRequestMessage(HttpMethod.Post, "general/v0/general");
        using var content = new MultipartFormDataContent
        {
            {
                new StreamContent(stream), "file", chunkInput.Name
            },
            {
                new StringContent(chunkInput.Language.ToUnstructuredChunkerLanguage()), "languages"
            },
        };
        //TODO experiment with this chunking strategy by maybe not using it
        content.Add(new StringContent("by_title"), "chunking_strategy");

        request.Content = content;

        var responseMessage = await _httpClient.SendAsync(request);
        return await responseMessage.Content.ReadFromJsonAsync<ChunkResult>() ?? throw new InvalidOperationException($"Cannot read {nameof(ChunkResult)}");
    }
}