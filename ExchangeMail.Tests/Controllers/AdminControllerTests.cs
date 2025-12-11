using System.Text;
using ExchangeMail.Web.Controllers;
using ExchangeMail.Web.Models;
using ExchangeMail.Core.Services;
using ExchangeMail.Core.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ExchangeMail.Tests.Controllers;

public class AdminControllerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IConfigurationService> _mockConfigurationService;
    private readonly Mock<IMailRepository> _mockMailRepository;
    private readonly Mock<ILogRepository> _mockLogRepository;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<ICalendarRepository> _mockCalendarRepository;
    private readonly Mock<IMailRuleService> _mockMailRuleService;
    private readonly AdminController _controller;

    private readonly Mock<HttpContext> _mockHttpContext;



    public AdminControllerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockConfigurationService = new Mock<IConfigurationService>();
        _mockMailRepository = new Mock<IMailRepository>();
        _mockLogRepository = new Mock<ILogRepository>();
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockCalendarRepository = new Mock<ICalendarRepository>();
        _mockMailRuleService = new Mock<IMailRuleService>();
        _controller = new AdminController(
            _mockUserRepository.Object,
            _mockConfigurationService.Object,
            _mockMailRepository.Object,
            _mockLogRepository.Object,
            _mockTaskRepository.Object,
            _mockCalendarRepository.Object,
            _mockMailRuleService.Object);

        _mockHttpContext = new Mock<HttpContext>();

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _mockHttpContext.Object
        };
    }

    private void SetupAdminSession(bool isAdmin)
    {
        if (isAdmin)
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "admin"),
                new System.Security.Claims.Claim("IsAdmin", "True")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
            var user = new System.Security.Claims.ClaimsPrincipal(identity);
            _mockHttpContext.Setup(c => c.User).Returns(user);
        }
        else
        {
            _mockHttpContext.Setup(c => c.User).Returns(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity()));
        }
    }

    [Fact]
    public async Task Index_RedirectsToLogin_WhenNotAdmin()
    {
        // Arrange
        SetupAdminSession(false);

        // Act
        var result = await _controller.Index();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirectResult.ActionName);
        Assert.Equal("Mail", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Index_ReturnsViewWithUsers_WhenAdmin()
    {
        // Arrange
        SetupAdminSession(true);
        var users = new List<UserEntity> { new UserEntity { Username = "test", Password = "password" } };
        _mockUserRepository.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(users);
        _mockConfigurationService.Setup(x => x.GetServerHeartbeatAsync()).ReturnsAsync(DateTime.UtcNow);



        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<User>>(viewResult.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task Create_Post_RedirectsToIndex_WhenSuccess()
    {
        // Arrange
        SetupAdminSession(true);
        var username = "newuser";
        var password = "password";
        var isAdmin = false;

        // Act
        var result = await _controller.Create(username, password, isAdmin);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _mockUserRepository.Verify(r => r.CreateUserAsync(username, password, isAdmin), Times.Once);
    }

    [Fact]
    public async Task Settings_Post_UpdatesDomain_WhenAdmin()
    {
        // Arrange
        SetupAdminSession(true);
        var domain = "example.com";
        var smtpPort = 25;
        var imapPort = 143;
        var smtpServer = "smtp.example.com";
        var imapServer = "imap.example.com";
        var enableSsl = true;

        // Act
        var result = await _controller.Settings(domain, smtpPort, smtpServer, imapPort, imapServer, "123", enableSsl, true, false, "OpenAI", "", "", "");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _mockConfigurationService.Verify(s => s.SetDomainAsync(domain), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_Post_RedirectsToIndex_WhenSuccess()
    {
        // Arrange
        SetupAdminSession(true);
        var username = "userToDelete";

        // Act
        var result = await _controller.DeleteUser(username);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _mockUserRepository.Verify(r => r.DeleteUserAsync(username), Times.Once);
    }
}
