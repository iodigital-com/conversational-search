using ConversationalSearchPlatform.BackOffice.Services.Models;
using HtmlAgilityPack;
using PuppeteerSharp;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class PuppeteerSharpScraperService : BaseScraper, IScraperService
{
    private readonly static string[] DefaultPuppeteerOptions =
    {
        "--disable-gpu", "--disable-dev-shm-usage", "--disable-setuid-sandbox", "--no-sandbox",
    };

    private readonly static Dictionary<string, string> ExtraDefaultHeaders = new()
    {
        {
            "accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7"
        },
    };

    private readonly ILogger<PuppeteerSharpScraperService> _logger;

    public PuppeteerSharpScraperService(ILogger<PuppeteerSharpScraperService> logger)
    {
        _logger = logger;
    }

    public async Task<ScrapeResult> ScrapeAsync(string url)
    {
        IBrowser? browser = null;
        string content;

        try
        {
            using var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
            browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                // Headless = true,
                Args = DefaultPuppeteerOptions,
            });

            var page = await browser.NewPageAsync();
            await page.SetJavaScriptEnabledAsync(true);
            // await page.SetUserAgentAsync("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36");
            // await page.SetCookieAsync(new List<CookieParam>
            // {
            //     new CookieParam
            //     {
            //         Name = "OptanonAlertBoxClosed",
            //         Value = "2023-10-09T10:32:39.421Z",
            //         Domain = ".polestar.com",
            //         Path = "/",
            //         HttpOnly = false,
            //         Secure = false
            //     },
            //     new CookieParam
            //     {
            //         Name = "projectAndYear",
            //         Value = "eyJwcm9qZWN0IjoicG9sZXN0YXItMiIsInllYXIiOiIyMDIyIn0=",
            //         Domain = ".www.polestar.com",
            //         Path = "/",
            //         HttpOnly = false,
            //         Secure = false
            //     },
            //     // new CookieParam
            //     // {
            //     //     Name = "OptanonConsent",
            //     //     Value =
            //     //         "isGpcEnabled=0&datestamp=Wed+Oct+18+2023+19%3A10%3A08+GMT%2B0200+(Central+European+Summer+Time)&version=202304.1.0&browserGpcFlag=0&isIABGlobal=false&hosts=&consentId=0b676ce3-b877-4e40-b5e4-85521e53b8aa&interactionCount=1&landingPath=NotLandingPage&groups=1%3A1%2C3%3A1%2C2%3A1%2C4%3A1&geolocation=BE%3BVLG&AwaitingReconsent=false",
            //     //     Domain = ".polestar.com",
            //     //     Path = "/",
            //     //     HttpOnly = false,
            //     //     Secure = false
            //     // }
            // }.ToArray());
            // await page.SetExtraHttpHeadersAsync(ExtraDefaultHeaders);


            await page.GoToAsync(url, WaitUntilNavigation.Networkidle2);
            await page.WaitForNetworkIdleAsync(new WaitForNetworkIdleOptions
            {
                IdleTime = 5000
            });
            content = await page.GetContentAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to start browser process");
            throw;
        }
        finally
        {
            if (browser is { IsClosed: false })
            {
                await browser.CloseAsync();
            }
        }


        var htmlDoc = new HtmlDocument();
        htmlDoc.Load(content);

        var imageScrapeParts = GetImageScrapeParts(htmlDoc);
        var pageTitle = GetPageTitle(htmlDoc);


        var html = htmlDoc.DocumentNode.OuterHtml;
        return new ScrapeResult(html ?? string.Empty, pageTitle ?? string.Empty, imageScrapeParts);
    }

}