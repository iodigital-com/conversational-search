using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IChunkService
{
    public Task<ChunkResult> ChunkAsync(ChunkInput chunkInput);
}