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
        var enableAutoLabeling = await _userRepository.GetAutoLabelingAsync(username);

        ViewBag.Signature = signature;
        ViewBag.EnableAnimations = enableAnimations;
        ViewBag.EnableAutoLabeling = enableAutoLabeling;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string signature, bool enableAnimations, bool enableAutoLabeling)
    {
        var username = GetCurrentUser();
        if (username == null) return RedirectToAction("Login", "Mail");

        await _userRepository.UpdateSignatureAsync(username, signature);
        await _userRepository.UpdateAnimationsAsync(username, enableAnimations);
        await _userRepository.UpdateAutoLabelingAsync(username, enableAutoLabeling);

        ViewBag.Signature = signature;
        ViewBag.EnableAnimations = enableAnimations;
        ViewBag.EnableAutoLabeling = enableAutoLabeling;
        ViewBag.Message = "Settings saved successfully.";

        return View();
    }
}
