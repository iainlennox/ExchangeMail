using HtmlAgilityPack;
using System;

var html = "<p>&lt;!DOCTYPE html&gt;</p><p>&lt;html&gt;...&lt;/html&gt;</p>";
var doc = new HtmlDocument();
doc.LoadHtml(html);
var text = doc.DocumentNode.InnerText;

Console.WriteLine($"Original: {html}");
Console.WriteLine($"InnerText: {text}");
Console.WriteLine($"Decoded: {HtmlEntity.DeEntitize(text)}");

if (text.Contains("<!DOCTYPE html>"))
{
    Console.WriteLine("InnerText decodes entities automatically.");
}
else
{
    Console.WriteLine("InnerText does NOT decode entities automatically.");
}
