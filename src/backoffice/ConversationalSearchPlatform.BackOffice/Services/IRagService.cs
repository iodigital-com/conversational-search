using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Services
{
    public interface IRagService
    {
        public Task<RAGDocument> GetRAGDocumentAsync(Guid tenantId);
    }
}
