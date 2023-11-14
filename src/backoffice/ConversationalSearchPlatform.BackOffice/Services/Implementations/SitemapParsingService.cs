using System.Net;
using ConversationalSearchPlatform.BackOffice.Services.Models.Sitemap;
using TurnerSoftware.RobotsExclusionTools;
using TurnerSoftware.SitemapTools;
using TurnerSoftware.SitemapTools.Parser;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class SitemapParsingService(SitemapQuery sitemapQuery, HttpClient httpClient) : ISitemapParsingService
{

    public async Task<SitemapFile> ParseFromFileAsync(Stream xmlStream, CancellationToken cancellationToken = default)
    {
        using (var streamReader = new StreamReader(xmlStream))
        {
            var parser = new XmlSitemapParser();
            var sitemap = await parser.ParseSitemapAsync(streamReader, cancellationToken);
            return sitemap;
        }
    }

    public async Task<SitemapFile> ParseFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var sitemapFile = await sitemapQuery.GetSitemapAsync(new Uri(url), cancellationToken);
        return sitemapFile;
    }

    public async Task<SiteMapDiscoveryResult> DiscoverFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var uris = await DiscoverInternal(url, cancellationToken);
        return new SiteMapDiscoveryResult(uris);
    }

    public async Task<Stream> DownloadAsStreamAsync(string url, CancellationToken cancellationToken = default)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    private async Task<HashSet<Uri>> DiscoverInternal(string url, CancellationToken cancellationToken)
    {
        var baseUri = ConstructBaseUri(url);

        var sitemapUris = new List<Uri>
        {
            baseUri
        };

        var robotsFile = await new RobotsFileParser(httpClient).FromUriAsync(baseUri, cancellationToken);

        sitemapUris.AddRange(robotsFile.SitemapEntries.Select(s => s.Sitemap));
        sitemapUris = sitemapUris.Distinct().ToList();

        var result = new HashSet<Uri>();

        foreach (var uri in sitemapUris)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Head, uri);
                var response = await httpClient.SendAsync(requestMessage, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    result.Add(uri);
                    continue;
                }

                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500 && response.StatusCode != HttpStatusCode.NotFound)
                {
                    requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                    response = await httpClient.SendAsync(requestMessage, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        result.Add(uri);
                    }
                }
            }
            catch (WebException)
            {
                // we're simply not adding it if it errors out
            }
        }

        return result;
    }

    private static Uri ConstructBaseUri(string url)
    {
        Uri baseUri;

        if (url.Contains("sitemap.xml"))
        {
            baseUri = new UriBuilder(url).Uri;
        }
        else
        {
            var uriBuilder = new UriBuilder(url);
            uriBuilder.Path = "sitemap.xml";
            baseUri = uriBuilder.Uri;
        }

        return baseUri;
    }
}