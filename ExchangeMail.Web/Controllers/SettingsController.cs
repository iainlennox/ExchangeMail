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

    private string? GetCurrentUser() => User.Identity?.Name;

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
        ViewBag.IsTwoFactorEnabled = await _userRepository.GetTwoFactorEnabledAsync(username);
        ViewBag.Message = "Settings saved successfully.";

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> EnableTwoFactor()
    {
        var username = GetCurrentUser();
        if (username == null) return RedirectToAction("Login", "Mail");

        // Generate a new secret
        var key = OtpNet.KeyGeneration.GenerateRandomKey(20);
        var secret = OtpNet.Base32Encoding.ToString(key);

        // Save it temporarily (or propose it)
        // We will store it in the DB but NOT enable it yet.
        await _userRepository.SetTwoFactorSecretAsync(username, secret);

        var userEmail = username + "@" + (await new SqliteConfigurationService(HttpContext.RequestServices.GetRequiredService<Core.Data.ExchangeMailContext>()).GetDomainAsync()); // Hacky, better to inject ConfigService

        // Generate QR Code URL (simple way using Google Charts API or similar for MVP, OR just pass secret)
        // For MVP locally, let's pass the secret and a constructed otpauth:// URI
        var otpAuthUri = $"otpauth://totp/ExchangeMail:{username}?secret={secret}&issuer=ExchangeMail";

        ViewBag.Secret = secret;
        ViewBag.OtpAuthUri = otpAuthUri;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmEnableTwoFactor(string code)
    {
        var username = GetCurrentUser();
        if (username == null) return RedirectToAction("Login", "Mail");

        var secret = await _userRepository.GetTwoFactorSecretAsync(username);
        if (string.IsNullOrEmpty(secret)) return RedirectToAction("Index");

        var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secret));
        bool valid = totp.VerifyTotp(code, out _, new OtpNet.VerificationWindow(2, 2));

        if (valid)
        {
            await _userRepository.SetTwoFactorEnabledAsync(username, true);
            TempData["SuccessMessage"] = "Two-Factor Authentication enabled successfully.";
            return RedirectToAction("Index");
        }

        ModelState.AddModelError("", "Invalid code. Please try again.");
        // Re-show the view with the same secret/QR
        ViewBag.Secret = secret;
        ViewBag.OtpAuthUri = $"otpauth://totp/ExchangeMail:{username}?secret={secret}&issuer=ExchangeMail";
        return View("EnableTwoFactor");
    }

    [HttpPost]
    public async Task<IActionResult> DisableTwoFactor()
    {
        var username = GetCurrentUser();
        if (username == null) return RedirectToAction("Login", "Mail");

        await _userRepository.SetTwoFactorEnabledAsync(username, false);
        await _userRepository.SetTwoFactorSecretAsync(username, ""); // Clear secret
        TempData["SuccessMessage"] = "Two-Factor Authentication disabled.";
        return RedirectToAction("Index");
    }
}
