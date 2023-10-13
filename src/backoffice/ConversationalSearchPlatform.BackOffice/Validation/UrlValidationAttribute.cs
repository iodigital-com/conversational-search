using System.ComponentModel.DataAnnotations;

namespace ConversationalSearchPlatform.BackOffice.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class UrlValidationAttribute : DataTypeAttribute
{
    public UrlValidationAttribute() : base(DataType.Url) =>
        ErrorMessage = "The {0} field is not a valid fully-qualified http, https URL.";

    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true;
        }

        return value is string valueAsString &&
               (valueAsString.StartsWith("http: //", StringComparison.OrdinalIgnoreCase) ||
                valueAsString.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
    }
}