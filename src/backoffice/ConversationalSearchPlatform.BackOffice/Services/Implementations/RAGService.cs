using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations
{
    public class RAGService : IRagService
    {
        private readonly Guid TENA_ID = Guid.Parse("CCFA9314-ABE6-403A-9E21-2B31D95A5258");
        private readonly Guid IODIGITAL_ID = Guid.Parse("4903E29F-D633-4A4C-9065-FE3DD8F27E40");

        public Task<RAGDocument> GetRAGDocumentAsync(Guid tenantId)
        {
            var ragDocument = new RAGDocument();

            if (tenantId == TENA_ID)
            {
                ragDocument.Classes.Add(new RAGClass()
                {
                    Description = "The following text sources contain information that is available on the Tena site. They form your knowledge base and thus extend and build upon the data you already have. Whenever a user asks a question about something that is contained within these documents, you can use the provided information to answer with certainty.",
                    Name = "Site",
                });

                ragDocument.Classes.Add(new RAGClass()
                {
                    Description = "These sources explicitely point to products that Tena sells. Use this information to provide the user with detailed information and a link to the product. A full overview of all products is available at: https://www.tenaprofessionals.us/professionals/products/",
                    Name = "Product",
                });
            }
            else if (tenantId == IODIGITAL_ID)
            {
                ragDocument.Classes.Add(new RAGClass()
                {
                    Description = "The following text sources contain information that is available on the site. They form your knowledge base and thus extend and build upon the data you already have. Whenever a user asks a question about something that is contained within these documents, you can use the provided information to answer with certainty.",
                    Name = "Site",
                });
            }
            else
            {
                ragDocument.Classes.Add(new RAGClass()
                {
                    Description = "The following text sources contain information that is available on the site. They form your knowledge base and thus extend and build upon the data you already have. Whenever a user asks a question about something that is contained within these documents, you can use the provided information to answer with certainty.",
                    Name = "Site",
                });
            }


            return Task.FromResult(ragDocument);
        }
    }
}
