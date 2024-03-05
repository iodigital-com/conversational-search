using System.Text;

namespace Jint.Fetch
{
    public static class FetchClass
    {
        public static async Task<FetchResult> Fetch(string uri)
        {
            return await FetchClass.Fetch(uri, FetchOptions.Default);
        }

        public static FetchOptions ExpandoToOptionsObject(object options)
        {
            FetchOptions fetchOptions = new();

            if (options is IDictionary<string, object> expandoOptions)
            {
                foreach (var key in expandoOptions.Keys)
                {
                    var normalizedKey = key.ToLowerInvariant();
                    switch (normalizedKey)
                    {
                        case "method":
                            expandoOptions.TryGetValue(key, out object methodObj);
                            fetchOptions.Method = methodObj as string ?? "GET";
                            break;
                        case "body":
                            expandoOptions.TryGetValue(key, out object bodyObj);
                            fetchOptions.Body = bodyObj;
                            break;
                    }
                }
            }

            return fetchOptions;
        }

        public static async Task<FetchResult> Fetch(string uri, FetchOptions fetchOptions)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(uri);

            HttpMethod httpMethod = fetchOptions.Method.ToLowerInvariant() switch
            {
                "get" => HttpMethod.Get,
                "put" => HttpMethod.Put,
                "post" => HttpMethod.Post,
                "delete" => HttpMethod.Delete,
                "options" => HttpMethod.Options,
                "head" => HttpMethod.Head,
                _ => HttpMethod.Get,
            };

            try
            {
                var httpRequestMessage = new HttpRequestMessage(httpMethod, uri);

                foreach (var header in fetchOptions.Headers)
                {
                    httpRequestMessage.Headers.Add(header.Key, header.Value);
                }

                if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
                {
                    if (fetchOptions.Body is string bodyString)
                    {
                        httpRequestMessage.Content = new StringContent(bodyString, Encoding.UTF8, "application/json");
                    }
                    else if (fetchOptions.Body is IDictionary<string, object> bodyObject)
                    {
                        Dictionary<string, string> keyValuePairs = bodyObject.ToDictionary(k => k.Key, k => k.Value.ToString() ?? "");
                        FormUrlEncodedContent formContent = new FormUrlEncodedContent(keyValuePairs);

                        httpRequestMessage.Content = formContent;
                    }
                }

                var response = await httpClient.SendAsync(httpRequestMessage);

                var result = new FetchResult(response);

                return result;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
    }
}
