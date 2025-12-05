using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ExchangeMail.Web.Models;
using ExchangeMail.Core.Services;

namespace ExchangeMail.Web.Controllers;

public class HomeController : Controller
{
    private readonly IUserRepository _userRepository;

    public HomeController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IActionResult> Index()
    {
        if (!await _userRepository.AnyUsersAsync())
        {
            return RedirectToAction("Intro", "Setup");
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    [HttpPost]
    public IActionResult KeepAlive()
    {
        return Ok();
    }
}
