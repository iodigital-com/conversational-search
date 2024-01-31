namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

using ConversationalSearchPlatform.BackOffice.Bootstrap;
using ConversationalSearchPlatform.BackOffice.Resources;
using ConversationalSearchPlatform.BackOffice.Services;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class KeywordExtractorService : IKeywordExtractorService
{
    private readonly ILogger<IKeywordExtractorService> _logger;
    private readonly LLamaSettings _lLamaSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    // todo: this should be an entity in the database
    private Dictionary<string, List<string>> _antonyms = new Dictionary<string, List<string>>()
    {
        { "light", new List<string>() { "heavy" } },
        { "heavy", new List<string>() { "light" } },
        { "man", new List<string>() { "female", "woman" } },
        { "men", new List<string>() { "females", "women" } },
        { "women", new List<string>() { "males", "men" } },
        { "woman", new List<string>() { "male", "man" } },
        { "elderly", new List<string>() { "young" } },
        { "old", new List<string>() { "young" } },
        { "young", new List<string>() { "old" } },
    };

    private class KeywordsResponse
    {
        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new();
    }

    private class LLamaRequest
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; } = 0.8f;


        [JsonPropertyName("max_tokens")]
        public float MaxTokens { get; set; } = 256;

        [JsonPropertyName("messages")]
        public List<LLamaMessage> Messages { get; set; } = new();
    }


    private class LLamaResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("choices")]
        public LLamaChoice[] Choices { get; set; } = null!;

        [JsonPropertyName("usage")]
        public LLamaUsage Usage { get; set; } = null!;
    }

    private class LLamaUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
    }

    private class LLamaChoice
    {
        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; } = string.Empty;
        [JsonPropertyName("index")]
        public int Index { get; set; }
        [JsonPropertyName("message")]
        public LLamaMessage Message { get; set; } = null!;
    }


    private class LLamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public KeywordExtractorService(ILogger<IKeywordExtractorService> logger,
        IOptions<LLamaSettings> lLamaSettings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _lLamaSettings = lLamaSettings.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<string>> ExtractKeywordAsync(string text)
    {
        var keywords = (await GetKeywordsFromLLMAsync(text)).Distinct().ToList();
        List<int> keywordsToRemove = new List<int>();

        for (int i = 0; i < keywords.Count; i++)
        {
            var keyword = keywords[i];

            if (_antonyms.ContainsKey(keyword))
            {
                var antonyms = _antonyms[keyword];
                for (int j = i; j < keywords.Count; j++)
                {
                    if (antonyms.Contains(keywords[j]))
                    {
                        keywordsToRemove.Add(i);
                    }
                }
            }
        }

        var removed = 0;
        foreach (var removeIndex in keywordsToRemove)
        {
            keywords.RemoveAt(removeIndex - removed);
            removed++;
        }

        return keywords;
    }

    private async Task<List<string>> GetKeywordsFromLLMAsync(string text)
    {
        var cleantext = RemoveSpecialCharacters(text);
        var words = cleantext.Split(' ');

        /*if (words.Length < 4)
        {*/
            return words.ToList();
        //}

        var keywordPrompt = await ResourceHelper.GetEmbeddedResourceTextAsync(ResourceHelper.KeywordsPromptFile);

        var httpClient = _httpClientFactory.CreateClient();

        var llamarequest = new LLamaRequest()
        {
            Messages = new List<LLamaMessage>()
            {
                new LLamaMessage()
                {
                    Content = keywordPrompt.Replace("{{textToProcess}}", text),
                },
            },
        };
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(_lLamaSettings.Endpoint),
            Headers = {
                { HttpRequestHeader.Authorization.ToString(), $"Bearer {_lLamaSettings.ApiToken}" },
                { HttpRequestHeader.Accept.ToString(), "application/json" },
            },
            Content = JsonContent.Create(llamarequest),
        };

        var response = await httpClient.SendAsync(httpRequestMessage);

        if (!response.IsSuccessStatusCode)
        {
            return words.ToList();
            /*throw new KeywordExtractionFailedException($"The LLama service replied with {response.StatusCode}",
                new Exception(await response.Content.ReadAsStringAsync()));*/
        }

        var llamaResponse = await response.Content.ReadFromJsonAsync<LLamaResponse>();

        if (llamaResponse == null || llamaResponse.Choices.Count() == 0)
        {
            return words.ToList();
            //throw new KeywordExtractionFailedException("The returned llama response is not valid");
        }

        var llamaResponseString = llamaResponse.Choices[0].Message.Content;

        int jsonStartIndex = llamaResponseString.IndexOf("{ \"keywords\"");
        int jsonEndIndex = llamaResponseString.LastIndexOf('}');

        if (jsonStartIndex == -1)
        {
            jsonStartIndex = llamaResponseString.IndexOf("{\"keywords\"");
        }

        if (jsonStartIndex == -1 || jsonEndIndex == -1 || jsonEndIndex < jsonStartIndex)
        {
            return words.ToList();
        }

        KeywordsResponse? keywordsResponse = null;

        try
        {
            keywordsResponse = JsonSerializer.Deserialize<KeywordsResponse>(llamaResponseString.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1));
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Error while deserializing: {llamaResponseString} with substring: {llamaResponseString.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1)}, {ex.InnerException}", ex);
        }

        if (keywordsResponse == null)
        {
            return words.ToList();
            //throw new KeywordExtractionFailedException($"The returned llama response does not containt keywords: {llamaResponse.Choices[0].Message.Content}");
        }

        return keywordsResponse.Keywords;
    }
    
    private static string RemoveSpecialCharacters(string str)
    {
        var sb = new StringBuilder();
        foreach (char c in str)
        {
            if ((c >= '0' && c <= '9') || char.IsLetter(c) || c == '\'' || c == ' ')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
