using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IChunkService
{
    public Task<ChunkCollection> ChunkAsync(ChunkInput chunkInput);
}