using System.Dynamic;
using System.Net.Http.Json;
using System.Text.Json;

namespace Jint.Fetch
{
    public class FetchResult
    {
        private readonly HttpResponseMessage _responseMessage;

        public FetchResult(HttpResponseMessage responseMessage)
        {
            _responseMessage = responseMessage;
        }


        public int Status => (int)_responseMessage.StatusCode;


        public async Task<string> Text()
        {
            return await _responseMessage.Content.ReadAsStringAsync();
        }

        public async Task<object?> Json()
        {
            try
            {
                JsonElement jsonResult = await _responseMessage.Content.ReadFromJsonAsync<JsonElement>();

                return ParseJsonElement(jsonResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return new object();
        }


        private object? ParseJsonElement(JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Array:
                    var newArray = new List<object?>();
                    foreach (var element in jsonElement.EnumerateArray())
                    {
                        newArray.Add(ParseJsonElement(element));
                    }

                    return newArray;
                case JsonValueKind.Object:
                    var newObj = new ExpandoObject() as IDictionary<string, object?>;

                    foreach (var property in jsonElement.EnumerateObject())
                    {
                        newObj.Add(property.Name, ParseJsonElement(property.Value));
                    }

                    return newObj;

                case JsonValueKind.String:
                    return jsonElement.GetString();
                case JsonValueKind.Number:
                    return jsonElement.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
            }


            return jsonElement;
        }

    }
}
