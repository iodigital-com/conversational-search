namespace ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Schemas;

public static class Schemas
{
    public static WeaviateCreateSchema ImageChunkSchema(string collectionName) =>
        new(collectionName,
            collectionName,
            "multi2vec-clip",
            new List<SchemaProperty>
            {
                new(new List<string>
                    {
                        "text"
                    },
                    "fileName",
                    "fileName"
                ),
                new(new List<string>
                    {
                        "text"
                    },
                    "Original alt description",
                    "altDescription"
                ),
                new(new List<string>
                    {
                        "text"
                    },
                    "nearByText",
                    "nearByText"
                ),
                new(new List<string>
                    {
                        "text"
                    },
                    "The internal ID of the webpage it belongs to",
                    "internalId"
                ),
                new(new List<string>
                    {
                        "text"
                    },
                    "Page title",
                    "title"
                ),
                new(new List<string>
                    {
                        "text"
                    },
                    "url",
                    "url"
                ),
                new(new List<string>
                    {
                        "blob"
                    },
                    "image",
                    "image"
                )
            },
            "hnsw",
            new ModuleConfig
            {
                Multi2VecClip = new Multi2VecClip
                {
                    ImageFields = new List<string>
                    {
                        "image"
                    }
                }
            }
        );

    public static WeaviateCreateSchema PageChunkSchema(string collectionName) =>
        new(collectionName,
            collectionName,
            "text2vec-transformers",
            new List<SchemaProperty>
            {
                new(new List<string>
                    {
                        "text"
                    },
                    "internalId",
                    "internalId"
                ),
                new(new List<string>
                    {
                        "text"
                    },
                    "ID of the tenant",
                    "tenantId"
                ),
                new(new List<string>
                    {
                        "text"
                    },
                    "text",
                    "text"
                ),
                new(new List<string>
                    {
                        "text"
                    },
                    "Product article number",
                    "articlenumber"
                ),
                new(new List<string>
                    {
                        "text"
                    },
                    "Product packaging",
                    "packaging"
                ),
                new(new List<string>
                    {
                        "text"
                    },
                    "Page title",
                    "title"
                ),
                new(new List<string>
                    {
                        "text"
                    },
                    "source",
                    "source"
                ),
                new(new List<string>
                    {
                        "text"
                    },
                    "language",
                    "language"
                ),
                new(new List<string>
                    {
                        "text"
                    },
                    "referenceType",
                    "referenceType"
                ),
            },
            null,
            null
        );

}