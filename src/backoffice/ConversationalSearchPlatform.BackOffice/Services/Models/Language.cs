using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Services.Models;

[Serializable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Language
{
    English,
    Swedish
}

public static class LanguageExtensions
{
    public static string ToUnstructuredChunkerLanguage(this Language language)
    {
        return language switch
        {
            Language.English => "eng",
            Language.Swedish => "swe",
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };
    }
}