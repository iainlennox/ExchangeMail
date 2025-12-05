using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace ExchangeMail.Core.Services;

public class HtmlSanitizerService
{
    public (string SanitizedHtml, bool IsContentBlocked) Sanitize(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return (string.Empty, false);
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        bool isContentBlocked = false;

        // Block external images
        var imgNodes = doc.DocumentNode.SelectNodes("//img");
        if (imgNodes != null)
        {
            foreach (var img in imgNodes)
            {
                var src = img.GetAttributeValue("src", "");
                if (!string.IsNullOrEmpty(src) && !src.StartsWith("data:") && !src.StartsWith("cid:"))
                {
                    img.SetAttributeValue("data-blocked-src", src);
                    img.SetAttributeValue("src", ""); // Or a placeholder image
                    isContentBlocked = true;
                }
            }
        }

        // Block external CSS (link tags)
        var linkNodes = doc.DocumentNode.SelectNodes("//link[@rel='stylesheet']");
        if (linkNodes != null)
        {
            foreach (var link in linkNodes)
            {
                link.Remove();
                isContentBlocked = true;
            }
        }

        // Block scripts
        var scriptNodes = doc.DocumentNode.SelectNodes("//script");
        if (scriptNodes != null)
        {
            foreach (var script in scriptNodes)
            {
                script.Remove();
                isContentBlocked = true;
            }
        }

        // Block iframes
        var iframeNodes = doc.DocumentNode.SelectNodes("//iframe");
        if (iframeNodes != null)
        {
            foreach (var iframe in iframeNodes)
            {
                iframe.Remove();
                isContentBlocked = true;
            }
        }

        // Block style attributes with url()
        var nodesWithStyle = doc.DocumentNode.SelectNodes("//*[@style]");
        if (nodesWithStyle != null)
        {
            foreach (var node in nodesWithStyle)
            {
                var style = node.GetAttributeValue("style", "");
                if (Regex.IsMatch(style, @"url\s*\(", RegexOptions.IgnoreCase))
                {
                    // Simple approach: remove the style attribute if it contains url()
                    // A more robust approach would be to parse the CSS and remove only the url() property
                    node.Attributes["style"].Remove();
                    isContentBlocked = true;
                }
            }
        }

        return (doc.DocumentNode.OuterHtml, isContentBlocked);
    }
}
