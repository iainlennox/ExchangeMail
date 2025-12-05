using ExchangeMail.Core.Services;
using ExchangeMail.Web.Models;
using Microsoft.AspNetCore.Mvc;
using ExchangeMail.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMail.Web.Controllers;

public class AdminController : Controller
{
    private readonly IMailRepository _mailRepository;
    private readonly IUserRepository _userRepository;
    private readonly IConfigurationService _configurationService;
    private readonly ILogRepository _logRepository;

    public AdminController(IUserRepository userRepository, IConfigurationService configurationService, IMailRepository mailRepository, ILogRepository logRepository)
    {
        _userRepository = userRepository;
        _configurationService = configurationService;
        _mailRepository = mailRepository;
        _logRepository = logRepository;
    }

    private bool IsAdmin()
    {
        return HttpContext.Session.GetString("IsAdmin") == "True";
    }

    public async Task<IActionResult> Index()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");

        var heartbeat = await _configurationService.GetServerHeartbeatAsync();
        bool isOnline = heartbeat.HasValue && heartbeat.Value > DateTime.UtcNow.AddMinutes(-2);
        ViewBag.ServerStatus = isOnline ? "Online" : "Offline";
        ViewBag.LastHeartbeat = heartbeat;

        // DIAGNOSTIC: Show connection string
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        ViewBag.ConnectionString = config.GetConnectionString("DefaultConnection") ?? "NULL (Using Fallback)";

        var users = await _userRepository.GetAllUsersAsync();
        var model = users.Select(u => new User
        {
            Username = u.Username,
            IsAdmin = u.IsAdmin,
            Password = u.Password
        });
        return View(model);
    }

    public IActionResult Create()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(string username, string password, bool isAdmin)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");
        try
        {
            await _userRepository.CreateUserAsync(username, password, isAdmin);
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View();
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(string username)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");
        await _userRepository.DeleteUserAsync(username);
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Settings()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");
        ViewBag.Domain = await _configurationService.GetDomainAsync();
        ViewBag.Port = await _configurationService.GetPortAsync();
        ViewBag.SmtpHost = await _configurationService.GetSmtpHostAsync();
        ViewBag.SmtpPort = await _configurationService.GetSmtpPortAsync();
        ViewBag.SmtpUsername = await _configurationService.GetSmtpUsernameAsync();
        ViewBag.SmtpPassword = await _configurationService.GetSmtpPasswordAsync();
        ViewBag.SmtpEnableSsl = await _configurationService.GetSmtpEnableSslAsync();
        ViewBag.InternalRoutingEnabled = await _configurationService.GetInternalRoutingEnabledAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Settings(string domain, int port, string smtpHost, int smtpPort, string smtpUsername, string smtpPassword, bool smtpEnableSsl, bool internalRoutingEnabled)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");
        await _configurationService.SetDomainAsync(domain);
        await _configurationService.SetPortAsync(port);
        await _configurationService.SetSmtpHostAsync(smtpHost);
        await _configurationService.SetSmtpPortAsync(smtpPort);
        await _configurationService.SetSmtpUsernameAsync(smtpUsername);
        await _configurationService.SetSmtpPasswordAsync(smtpPassword);
        await _configurationService.SetSmtpEnableSslAsync(smtpEnableSsl);
        await _configurationService.SetInternalRoutingEnabledAsync(internalRoutingEnabled);
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Messages(int page = 1)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");
        int pageSize = 20;
        var (messages, totalCount) = await _mailRepository.GetAllMessagesAsync(page, pageSize);

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return View(messages);
    }

    public async Task<IActionResult> Logs(int page = 1)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");
        int pageSize = 20;
        var (logs, totalCount) = await _logRepository.GetLogsAsync(page, pageSize);

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return View(logs);
    }

    [HttpPost]
    public async Task<IActionResult> ClearLogs()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");
        await _logRepository.ClearLogsAsync();
        return RedirectToAction("Logs");
    }
    [HttpGet]
    public IActionResult ResetDatabase()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> PerformResetDatabase(string confirmation)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");

        if (confirmation != "I understand")
        {
            ModelState.AddModelError("", "You must type 'I understand' to confirm.");
            return View("ResetDatabase");
        }

        // We need the DbContext to perform the reset. 
        // Since it's not injected directly into the controller, we should probably inject it or add a method to a repository.
        // However, this is a very specific admin action. Let's inject the context for this purpose.
        // Wait, I can't easily change the constructor without updating tests and DI.
        // Let's see if I can use RequestServices to get it, or better, add a method to IConfigurationService or a new IAdminService.
        // Given the constraints and the "quick" nature of this task, let's resolve it from HttpContext.RequestServices.

        var dbContext = HttpContext.RequestServices.GetRequiredService<ExchangeMail.Core.Data.ExchangeMailContext>();

        // Execute delete commands for specific tables
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Messages");
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Folders");
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Contacts");
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Logs");
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM SafeSenders");
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM BlockedSenders");
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Users");

        // Vacuum to reclaim space
        await dbContext.Database.ExecuteSqlRawAsync("VACUUM");

        // Clear session
        HttpContext.Session.Clear();

        return RedirectToAction("Index", "Setup");
    }
}
