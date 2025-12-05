using ExchangeMail.Core.Services;
using Xunit;

namespace ExchangeMail.Tests.Services;

public class HtmlSanitizerServiceTests
{
    private readonly HtmlSanitizerService _service;

    public HtmlSanitizerServiceTests()
    {
        _service = new HtmlSanitizerService();
    }

    [Fact]
    public void Sanitize_ShouldBlockExternalImages()
    {
        var html = "<img src='http://example.com/image.jpg' />";
        var (sanitized, isBlocked) = _service.Sanitize(html);

        Assert.True(isBlocked);
        Assert.Contains("data-blocked-src=\"http://example.com/image.jpg\"", sanitized);
        // HtmlAgilityPack preserves original quotes
        Assert.Contains("src=''", sanitized);
    }

    [Fact]
    public void Sanitize_ShouldAllowDataImages()
    {
        var html = "<img src='data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==' />";
        var (sanitized, isBlocked) = _service.Sanitize(html);

        Assert.False(isBlocked);
        Assert.Contains("src='data:image/png;base64,", sanitized);
    }

    [Fact]
    public void Sanitize_ShouldRemoveScripts()
    {
        var html = "<script>alert('xss')</script><p>Hello</p>";
        var (sanitized, isBlocked) = _service.Sanitize(html);

        Assert.True(isBlocked);
        Assert.DoesNotContain("<script>", sanitized);
        Assert.Contains("<p>Hello</p>", sanitized);
    }

    [Fact]
    public void Sanitize_ShouldRemoveIframes()
    {
        var html = "<iframe src='http://example.com'></iframe>";
        var (sanitized, isBlocked) = _service.Sanitize(html);

        Assert.True(isBlocked);
        Assert.DoesNotContain("<iframe", sanitized);
    }

    [Fact]
    public void Sanitize_ShouldRemoveExternalCss()
    {
        var html = "<link rel='stylesheet' href='http://example.com/style.css' />";
        var (sanitized, isBlocked) = _service.Sanitize(html);

        Assert.True(isBlocked);
        Assert.DoesNotContain("<link", sanitized);
    }
}
