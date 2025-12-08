using ExchangeMail.Core.Data.Entities;
using ExchangeMail.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeMail.Web.Controllers;

public class CalendarController : Controller
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly IConfigurationService _configurationService;

    public CalendarController(ICalendarRepository calendarRepository, IConfigurationService configurationService)
    {
        _calendarRepository = calendarRepository;
        _configurationService = configurationService;
    }

    private string? GetCurrentUser() => HttpContext.Session.GetString("Username");

    private async Task<string> GetUserEmailAsync()
    {
        var username = GetCurrentUser();
        if (string.IsNullOrEmpty(username)) return string.Empty;

        var domain = await _configurationService.GetDomainAsync();
        var localPart = username.Contains("@") ? username.Split('@')[0] : username;
        return $"{localPart}@{domain}";
    }

    public async Task<IActionResult> Index()
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login", "Mail");
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents(DateTime start, DateTime end)
    {
        var username = GetCurrentUser();
        if (username == null) return Unauthorized();

        var userEmail = await GetUserEmailAsync();
        var events = await _calendarRepository.GetEventsAsync(userEmail, start, end);

        var jsonEvents = events.Select(e => new
        {
            id = e.Id,
            title = e.Subject,
            start = e.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
            end = e.EndDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
            location = e.Location,
            description = e.Description,
            allDay = e.IsAllDay
        });

        return Json(jsonEvents);
    }

    [HttpPost]
    public async Task<IActionResult> SaveEvent([FromBody] CalendarEventEntity model)
    {
        var username = GetCurrentUser();
        if (username == null) return Unauthorized();

        // Since UserEmail is required in the DB/Entity but not sent from client, we remove it from ModelState
        ModelState.Remove("UserEmail");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userEmail = await GetUserEmailAsync();

        if (model.Id == 0)
        {
            model.UserEmail = userEmail;
            await _calendarRepository.AddEventAsync(model);
        }
        else
        {
            var existing = await _calendarRepository.GetEventAsync(model.Id);
            if (existing == null) return NotFound();
            if (existing.UserEmail != userEmail) return Forbid();

            existing.Subject = model.Subject;
            existing.StartDateTime = model.StartDateTime;
            existing.EndDateTime = model.EndDateTime;
            existing.Location = model.Location;
            existing.Description = model.Description;
            existing.IsAllDay = model.IsAllDay;

            await _calendarRepository.UpdateEventAsync(existing);
        }

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var username = GetCurrentUser();
        if (username == null) return Unauthorized();

        var userEmail = await GetUserEmailAsync();
        var existing = await _calendarRepository.GetEventAsync(id);

        if (existing == null) return NotFound();
        if (existing.UserEmail != userEmail) return Forbid();

        await _calendarRepository.DeleteEventAsync(id);
        return Ok();
    }
}
