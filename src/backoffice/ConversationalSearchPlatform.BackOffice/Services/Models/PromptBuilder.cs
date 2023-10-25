using System.Text;
using ConversationalSearchPlatform.BackOffice.Tenants;

namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public class PromptBuilder(StringBuilder stringBuilder)
{

    private List<(string key, string value)> ReplacedPromptTags = new();

    public PromptBuilder ReplaceTenantPrompt(string tenantPrompt)
    {
        stringBuilder = stringBuilder.Replace("{{TenantPrompt}}", tenantPrompt);
        return this;
    }

    public PromptBuilder ReplaceTextSources(string flattenedTextSources)
    {
        stringBuilder = stringBuilder.Replace("{{TextSources}}", flattenedTextSources);
        return this;
    }

    public PromptBuilder ReplaceImageSources(string flattenedImageSources)
    {
        stringBuilder = stringBuilder.Replace("{{ImageSources}}", flattenedImageSources);
        return this;
    }

    public PromptBuilder ReplaceConversationContextVariables(List<PromptTag> promptTags, IDictionary<string, string> conversationContext)
    {
        foreach (var promptTag in promptTags)
        {
            var trimmedKey = promptTag.Value.TrimStart('{').TrimEnd('}');
            conversationContext.TryGetValue(trimmedKey, out var value);

            if (value != null)
            {
                stringBuilder = stringBuilder.Replace(promptTag.Value, value);
                ReplacedPromptTags.Add(new ValueTuple<string, string>(promptTag.Value, value));
            }
        }

        return this;
    }

    public IEnumerable<(string key, string value)> GetReplacedPromptTags() => ReplacedPromptTags;

    public string Build() => stringBuilder.ToString();
}