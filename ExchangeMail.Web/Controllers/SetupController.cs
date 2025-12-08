using Microsoft.AspNetCore.Mvc;
using ExchangeMail.Web.Models;
using ExchangeMail.Core.Services;

namespace ExchangeMail.Web.Controllers;

public class SetupController : Controller
{
    private readonly IConfigurationService _configurationService;
    private readonly IUserRepository _userRepository;
    private readonly IMailRepository _mailRepository;

    public SetupController(IConfigurationService configurationService, IUserRepository userRepository, IMailRepository mailRepository)
    {
        _configurationService = configurationService;
        _userRepository = userRepository;
        _mailRepository = mailRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Intro()
    {
        if (await _userRepository.AnyUsersAsync())
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (await _userRepository.AnyUsersAsync())
        {
            return RedirectToAction("Index", "Home");
        }

        var model = new SetupViewModel
        {
            Domain = await _configurationService.GetDomainAsync(),
            SmtpHost = await _configurationService.GetSmtpHostAsync(),
            SmtpPort = await _configurationService.GetSmtpPortAsync(),
            SmtpUsername = await _configurationService.GetSmtpUsernameAsync(),
            SmtpPassword = await _configurationService.GetSmtpPasswordAsync(),
            SmtpEnableSsl = await _configurationService.GetSmtpEnableSslAsync()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(SetupViewModel model)
    {
        if (await _userRepository.AnyUsersAsync())
        {
            return RedirectToAction("Index", "Home");
        }

        if (ModelState.IsValid)
        {
            await _configurationService.SetDomainAsync(model.Domain);
            await _configurationService.SetSmtpHostAsync(model.SmtpHost);
            await _configurationService.SetSmtpPortAsync(model.SmtpPort);
            await _configurationService.SetSmtpUsernameAsync(model.SmtpUsername);
            await _configurationService.SetSmtpPasswordAsync(model.SmtpPassword);
            await _configurationService.SetSmtpEnableSslAsync(model.SmtpEnableSsl);

            return RedirectToAction("AddAccount");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AddAccount()
    {
        if (await _userRepository.AnyUsersAsync())
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddAccount(string username, string password)
    {
        if (await _userRepository.AnyUsersAsync())
        {
            return RedirectToAction("Index", "Home");
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Username and Password are required.");
            return View();
        }

        try
        {
            await _userRepository.CreateUserAsync(username, password, true);

            // Send Welcome Email
            var welcomeParams = new List<(string UserEmail, string? Folder, string? Labels)> { (username, null, null) };
            var welcomeEmail = WelcomeEmailGenerator.Create(username);

            // Set Date to now
            welcomeEmail.Date = DateTimeOffset.Now;

            await _mailRepository.SaveMessageWithUserStatesAsync(welcomeEmail, welcomeParams);

            // Auto-login
            HttpContext.Session.SetString("Username", username);
            HttpContext.Session.SetString("IsAdmin", "True");

            return RedirectToAction("Complete");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View();
        }
    }

    [HttpGet]
    public IActionResult Complete()
    {
        return View();
    }
}
