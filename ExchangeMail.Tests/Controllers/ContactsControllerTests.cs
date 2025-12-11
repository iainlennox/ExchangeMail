using System.Text;
using ExchangeMail.Web.Controllers;
using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ExchangeMail.Tests.Controllers;

public class ContactsControllerTests
{
    private readonly ExchangeMailContext _context;
    private readonly ContactsController _controller;

    private readonly Mock<HttpContext> _mockHttpContext;

    public ContactsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ExchangeMailContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;
        _context = new ExchangeMailContext(options);

        _controller = new ContactsController(_context);

        _mockHttpContext = new Mock<HttpContext>();
        var defaultUser = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
        _mockHttpContext.Setup(c => c.User).Returns(defaultUser);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _mockHttpContext.Object
        };
    }

    private void SetupUserSession(string username)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username)
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
        var user = new System.Security.Claims.ClaimsPrincipal(identity);
        _mockHttpContext.Setup(c => c.User).Returns(user);
    }

    [Fact]
    public async Task Index_RedirectsToLogin_WhenNotLoggedIn()
    {
        // Arrange
        // No session setup

        // Act
        var result = await _controller.Index();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirectResult.ActionName);
        Assert.Equal("Mail", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Index_ReturnsView_WhenLoggedIn()
    {
        // Arrange
        SetupUserSession("testuser");

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsAssignableFrom<IEnumerable<ContactEntity>>(viewResult.Model);
    }

    [Fact]
    public async Task Details_RedirectsToLogin_WhenNotLoggedIn()
    {
        // Arrange
        // No session setup

        // Act
        var result = await _controller.Details(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirectResult.ActionName);
        Assert.Equal("Mail", redirectResult.ControllerName);
    }

    [Fact]
    public void Create_Get_RedirectsToLogin_WhenNotLoggedIn()
    {
        // Arrange
        // No session setup

        // Act
        var result = _controller.Create(null, null);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirectResult.ActionName);
        Assert.Equal("Mail", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Create_Post_RedirectsToLogin_WhenNotLoggedIn()
    {
        // Arrange
        // No session setup
        var contact = new ContactEntity { Name = "Test", Email = "test@example.com" };

        // Act
        var result = await _controller.Create(contact);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirectResult.ActionName);
        Assert.Equal("Mail", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Edit_Get_RedirectsToLogin_WhenNotLoggedIn()
    {
        // Arrange
        // No session setup

        // Act
        var result = await _controller.Edit(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirectResult.ActionName);
        Assert.Equal("Mail", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Edit_Post_RedirectsToLogin_WhenNotLoggedIn()
    {
        // Arrange
        // No session setup
        var contact = new ContactEntity { Id = 1, Name = "Test", Email = "test@example.com" };

        // Act
        var result = await _controller.Edit(1, contact);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirectResult.ActionName);
        Assert.Equal("Mail", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Delete_Get_RedirectsToLogin_WhenNotLoggedIn()
    {
        // Arrange
        // No session setup

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirectResult.ActionName);
        Assert.Equal("Mail", redirectResult.ControllerName);
    }

    [Fact]
    public async Task DeleteConfirmed_RedirectsToLogin_WhenNotLoggedIn()
    {
        // Arrange
        // No session setup

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirectResult.ActionName);
        Assert.Equal("Mail", redirectResult.ControllerName);
    }
}
