using System.Text;
using ConversationalSearchPlatform.BackOffice.Tenants;

namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public class PromptBuilder(StringBuilder stringBuilder)
{
    private readonly List<(string key, string value)> _replacedPromptTags = new();

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

    public PromptBuilder ReplaceProductSources(string flattenedProductSources)
    {
        stringBuilder = stringBuilder.Replace("{{ProductSources}}", flattenedProductSources);
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
                _replacedPromptTags.Add(new ValueTuple<string, string>(promptTag.Value, value));
            }
        }

        return this;
    }

    public IEnumerable<(string key, string value)> GetReplacedPromptTags() => _replacedPromptTags;

    public string Build() => stringBuilder.ToString();
}