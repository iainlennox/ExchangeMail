using ExchangeMail.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeMail.Web.Controllers;

public class SettingsController : Controller
{
    private readonly IUserRepository _userRepository;

    public SettingsController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    private string? GetCurrentUser() => HttpContext.Session.GetString("Username");

    public async Task<IActionResult> Index()
    {
        var username = GetCurrentUser();
        if (username == null) return RedirectToAction("Login", "Mail");

        var signature = await _userRepository.GetSignatureAsync(username);
        var enableAnimations = await _userRepository.GetAnimationsAsync(username);

        ViewBag.Signature = signature;
        ViewBag.EnableAnimations = enableAnimations;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string signature, bool enableAnimations)
    {
        var username = GetCurrentUser();
        if (username == null) return RedirectToAction("Login", "Mail");

        await _userRepository.UpdateSignatureAsync(username, signature);
        await _userRepository.UpdateAnimationsAsync(username, enableAnimations);

        ViewBag.Signature = signature;
        ViewBag.EnableAnimations = enableAnimations;
        ViewBag.Message = "Settings saved successfully.";

        return View();
    }
}
