using ExchangeMail.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ExchangeMail.Web.Controllers;

public class OutlookController : Controller
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IMailRepository _mailRepository;
    private readonly IAiEmailService _aiEmailService;
    private readonly IConfigurationService _configurationService;

    public OutlookController(
        ICalendarRepository calendarRepository,
        ITaskRepository taskRepository,
        IMailRepository mailRepository,
        IAiEmailService aiEmailService,
        IConfigurationService configurationService)
    {
        _calendarRepository = calendarRepository;
        _taskRepository = taskRepository;
        _mailRepository = mailRepository;
        _aiEmailService = aiEmailService;
        _configurationService = configurationService;
    }

    private string? GetCurrentUser() => User.Identity?.Name;

    private async Task<string> GetUserEmailAsync()
    {
        var username = GetCurrentUser();
        if (string.IsNullOrEmpty(username)) return string.Empty;

        var domain = await _configurationService.GetDomainAsync();
        var localPart = username.Contains("@") ? username.Split('@')[0] : username;
        return $"{localPart}@{domain}";
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary()
    {
        var username = GetCurrentUser();
        if (username == null) return Unauthorized();

        var userEmail = await GetUserEmailAsync();
        var today = DateTime.Today;

        // 1. Get Today's Events
        var events = await _calendarRepository.GetEventsAsync(userEmail, today, today.AddDays(1).AddTicks(-1));
        var eventList = events.OrderBy(e => e.StartDateTime).ToList();

        // 2. Get Pending Tasks (Due today or Overdue, or all active if we want)
        // For briefing, let's focus on what's actionable NOW.
        // We'll fetch all active tasks and filter in memory for simplicity or usage of existing repo methods.
        var allTasks = await _taskRepository.GetTasksAsync(userEmail, filterType: "active");
        var relevantTasks = allTasks
            .Where(t => t.DueDate.HasValue && t.DueDate.Value.Date <= today) // Due today or overdue
            .OrderBy(t => t.DueDate)
            .ToList();

        // 3. Get Important Emails (Unread + Focused?)
        // Fetch page 1 of Inbox, Focused
        var (messages, _) = await _mailRepository.GetMessagesAsync(userEmail, "", 1, 10, "Inbox", true);
        var unreadEmails = messages
            .Where(m => m.Headers.Contains("X-Is-Read") == false) // simplified check based on repo logic adding headers
            .Take(5)
            .ToList();

        // 4. Build Context for AI
        var sb = new StringBuilder();

        sb.AppendLine("<h2>Calendar Events for Today</h2>");
        if (eventList.Any())
        {
            sb.AppendLine("<ul>");
            foreach (var e in eventList)
            {
                sb.AppendLine($"<li>{e.StartDateTime:HH:mm} - {e.Subject} ({e.Location})</li>");
            }
            sb.AppendLine("</ul>");
        }
        else
        {
            sb.AppendLine("<p>No events scheduled for today.</p>");
        }

        sb.AppendLine("<h2>Tasks Due</h2>");
        if (relevantTasks.Any())
        {
            sb.AppendLine("<ul>");
            foreach (var t in relevantTasks)
            {
                var due = t.DueDate!.Value.Date < today ? "Overdue" : "Today";
                sb.AppendLine($"<li>[{due}] {t.Subject} (Priority: {t.Priority})</li>");
            }
            sb.AppendLine("</ul>");
        }
        else
        {
            sb.AppendLine("<p>No pending tasks due today.</p>");
        }

        sb.AppendLine("<h2>Unread Important Emails</h2>");
        if (unreadEmails.Any())
        {
            sb.AppendLine("<ul>");
            foreach (var m in unreadEmails)
            {
                sb.AppendLine($"<li>From: {m.From}, Subject: {m.Subject}</li>");
            }
            sb.AppendLine("</ul>");
        }
        else
        {
            sb.AppendLine("<p>No unread important emails.</p>");
        }

        // 5. Generate AI Briefing
        var timeOfDay = DateTime.Now.Hour < 12 ? "morning" :
                        DateTime.Now.Hour < 17 ? "afternoon" : "evening";

        string aiSummary = await _aiEmailService.GenerateDailyBriefingAsync(sb.ToString(), timeOfDay);
        string greeting = $"{char.ToUpper(timeOfDay[0]) + timeOfDay.Substring(1)} Briefing";

        // Return Data
        return Json(new
        {
            summary = aiSummary,
            greeting = greeting,
            events = eventList.Select(e => new { time = e.StartDateTime.ToString("HH:mm"), subject = e.Subject, location = e.Location }),
            tasks = relevantTasks.Select(t => new { subject = t.Subject, priority = t.Priority, overdue = t.DueDate.Value.Date < today }),
            emails = unreadEmails.Select(m => new { from = m.From.ToString(), subject = m.Subject ?? "(No Subject)" })
        });
    }
}
