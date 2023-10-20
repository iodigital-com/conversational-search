using ConversationalSearchPlatform.BackOffice.Tenants;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace ConversationalSearchPlatform.BackOffice.Data;

public class PromptTagConverter : ValueConverter<List<PromptTag>?, string?>
{
    public PromptTagConverter() : base(
        d => d == null
            ? default
            : JsonConvert.SerializeObject(d),
        d => d == null
            ? default
            : JsonConvert.DeserializeObject<List<PromptTag>>(d))
    {
    }
}