using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Services.Models;

[Serializable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChatModel
{
    Gpt35Turbo = 350,
    Gpt35Turbo_16K = 352,
    Gpt4 = 400,
    Gpt4_32K = 402,
    Gpt4_turbo = 1106,
}