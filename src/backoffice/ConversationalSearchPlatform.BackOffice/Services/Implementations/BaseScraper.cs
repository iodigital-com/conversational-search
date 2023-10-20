using ConversationalSearchPlatform.BackOffice.Services.Models;
using HtmlAgilityPack;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public abstract class BaseScraper
{
    protected static List<ImageScrapePart> GetImageScrapeParts(HtmlDocument htmlDoc)
    {
        var imageScrapeParts = new List<ImageScrapePart>();
        var nodes = htmlDoc.DocumentNode.SelectNodes("//img");

        if (nodes == null)
            return imageScrapeParts;

        foreach (var node in nodes)
        {
            var src = node.GetAttributeValue("src", null);
            var alt = node.GetAttributeValue("alt", null);
            var nearbyText = GetNearbyText(node, 5);


            imageScrapeParts.Add(new ImageScrapePart(src, alt, nearbyText));
        }

        return imageScrapeParts;
    }

    private static string? GetNearbyText(HtmlNode node, int maxSearchRadius)
    {
        string? nearbyText = null;

        if (node.HasChildNodes)
        {
            nearbyText = node.FirstChild.InnerText;
        }
        else
        {
            var next = node.NextSibling;
            var counter = 0;

            while (next != null && next.NodeType != HtmlNodeType.Text && counter < maxSearchRadius)
            {
                next = next.NextSibling;
                counter++;
            }

            if (next != null)
            {
                nearbyText = next.InnerText;
            }

            var previous = node.PreviousSibling;
            counter = 0;

            while (previous != null && previous.NodeType != HtmlNodeType.Text && counter < maxSearchRadius)
            {
                previous = previous.PreviousSibling;
                counter++;
            }

            if (previous != null)
            {
                nearbyText = previous.InnerText;
            }
        }

        return nearbyText;
    }
}