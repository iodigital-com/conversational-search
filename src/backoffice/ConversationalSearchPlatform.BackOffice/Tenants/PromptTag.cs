using System.ComponentModel.DataAnnotations.Schema;

namespace ConversationalSearchPlatform.BackOffice.Tenants;

[ComplexType]
public class PromptTag
{
    public string Value { get; set; } = default!;
}