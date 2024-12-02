using System.Xml;

namespace ConversationalSearchPlatform.BackOffice.Services.Models
{
    public class RAGDocument
    {
        public List<RAGClass> Classes { get; set; } = new List<RAGClass>();

        public async Task<string> GenerateXMLStringAsync()
        {
            using var stream = new MemoryStream();
            using var streamReader = new StreamReader(stream);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Async = true;

            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                await writer.WriteStartElementAsync(null, "SOURCES", null);

                foreach (var ragClass in Classes)
                {
                    await writer.WriteStartElementAsync(null, ragClass.Name.ToUpper(), null);

                    await writer.WriteStartElementAsync(null, nameof(ragClass.Description).ToUpper(), null);
                    await writer.WriteStringAsync(ragClass.Description);
                    await writer.WriteEndElementAsync();

                    foreach (var ragSource in ragClass.Sources)
                    {
                        await writer.WriteStartElementAsync(null, "Reference", null);

                        await writer.WriteElementStringAsync(null, nameof(ragSource.ReferenceId), null, $"{ragSource.ReferenceId}");

                        foreach (var ragProperty in ragSource.Properties)
                        {
                            await writer.WriteElementStringAsync(null, ragProperty.Key, null, ragProperty.Value);
                        }


                        await writer.WriteEndElementAsync();
                    }


                    await writer.WriteEndElementAsync();
                }

                await writer.WriteEndElementAsync();
                await writer.FlushAsync();
            }

            stream.Position = 0;
            return await streamReader.ReadToEndAsync();
        }
    }

    public class RAGClass
    {
        public string Description { get; set; } = "";

        public string Name { get; set; } = "";

        public List<RAGSource> Sources { get; set; } = new List<RAGSource>();
    }

    public class RAGSource
    {
        public int ReferenceId { get; set; } = 0;

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }


}
