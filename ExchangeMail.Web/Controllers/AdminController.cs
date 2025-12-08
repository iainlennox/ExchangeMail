using ExchangeMail.Core.Services;
using ExchangeMail.Web.Models;
using Microsoft.AspNetCore.Mvc;
using ExchangeMail.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace ExchangeMail.Web.Controllers;

public class AdminController : Controller
{
    private readonly IMailRepository _mailRepository;
    private readonly IUserRepository _userRepository;
    private readonly IConfigurationService _configurationService;
    private readonly ILogRepository _logRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICalendarRepository _calendarRepository;

    public AdminController(IUserRepository userRepository, IConfigurationService configurationService, IMailRepository mailRepository, ILogRepository logRepository, ITaskRepository taskRepository, ICalendarRepository calendarRepository)
    {
        _userRepository = userRepository;
        _configurationService = configurationService;
        _mailRepository = mailRepository;
        _logRepository = logRepository;
        _taskRepository = taskRepository;
        _calendarRepository = calendarRepository;
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

            // Send Welcome Email
            var welcomeParams = new List<(string UserEmail, string? Folder, string? Labels)> { (username, null, null) };
            var welcomeEmail = WelcomeEmailGenerator.Create(username);

            // Set Date to now
            welcomeEmail.Date = DateTimeOffset.Now;

            await _mailRepository.SaveMessageWithUserStatesAsync(welcomeEmail, welcomeParams);

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

    [HttpPost]
    public async Task<IActionResult> GenerateDemoData(string username)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");

        try
        {
            var today = DateTime.Today;

            // 1. Calendar Events
            var events = new List<CalendarEventEntity>
            {
                new CalendarEventEntity { UserEmail = username, Subject = "Team Standup", StartDateTime = today.AddHours(9), EndDateTime = today.AddHours(9.5), Location = "Conference Room A", Description = "Daily sync" },
                new CalendarEventEntity { UserEmail = username, Subject = "Project Review", StartDateTime = today.AddHours(14), EndDateTime = today.AddHours(15), Location = "Online", Description = "Review Q4 goals" }
            };

            foreach (var evt in events)
            {
                await _calendarRepository.AddEventAsync(evt);
            }

            // 2. Tasks
            var tasks = new List<TaskEntity>
            {
                new TaskEntity { UserEmail = username, Subject = "Submit Expense Report", IsCompleted = false, DueDate = today.AddDays(-1), Priority = 2 }, // Overdue
                new TaskEntity { UserEmail = username, Subject = "Prepare Presentation", IsCompleted = false, DueDate = today, Priority = 3 } // High priority today
            };

            foreach (var t in tasks)
            {
                await _taskRepository.AddTaskAsync(t);
            }

            // 3. Emails
            var demoEmails = new List<(string Subject, string From, string Body, bool Urgent)>
            {
                ("Urgent: Server Update Required", "ops@company.com", "Please update the server by EOD.", true),
                ("Welcome to the team!", "hr@company.com", "We are glad to have you here.", false),
                ("Meeting Notes", "manager@company.com", "Here are the notes from yesterday not-to-be-missed.", false)
            };

            foreach (var emailData in demoEmails)
            {
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress("Demo Sender", emailData.From));
                mimeMessage.To.Add(new MailboxAddress(username, username));
                mimeMessage.Subject = emailData.Subject;
                mimeMessage.Body = new TextPart("plain") { Text = emailData.Body };
                mimeMessage.Date = DateTimeOffset.Now.AddMinutes(-new Random().Next(1, 120));

                string? labels = emailData.Urgent ? "Urgent" : null;
                var userStates = new List<(string UserEmail, string? Folder, string? Labels)> { (username, "Inbox", labels) };
                await _mailRepository.SaveMessageWithUserStatesAsync(mimeMessage, userStates);
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            // Ideally use TempData to show error
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> ClearDemoData(string username)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");

        try
        {
            var userTasks = await _taskRepository.GetTasksAsync(username, true);
            var demoTaskSubjects = new[] { "Submit Expense Report", "Prepare Presentation" };
            foreach (var t in userTasks.Where(x => demoTaskSubjects.Contains(x.Subject)))
            {
                await _taskRepository.DeleteTaskAsync(t.Id);
            }

            var today = DateTime.Today;
            var userEvents = await _calendarRepository.GetEventsAsync(username, today, today.AddDays(1));
            var demoEventSubjects = new[] { "Team Standup", "Project Review" };
            foreach (var e in userEvents.Where(x => demoEventSubjects.Contains(x.Subject)))
            {
                await _calendarRepository.DeleteEventAsync(e.Id);
            }

            var (userMessages, _) = await _mailRepository.GetMessagesAsync(username, "", 1, 100);
            var demoEmailSubjects = new[] { "Urgent: Server Update Required", "Welcome to the team!", "Meeting Notes" };

            foreach (var msg in userMessages.Where(m => demoEmailSubjects.Contains(m.Subject)))
            {
                var idHeader = msg.Headers["X-Db-Id"];
                if (!string.IsNullOrEmpty(idHeader))
                {
                    await _mailRepository.PermanentDeleteMessageAsync(idHeader, username);
                }
            }
        }
        catch (Exception)
        {
            // Ignore errors
        }

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

        // Summarization Settings
        ViewBag.SummarizationEnabled = await _configurationService.GetSummarizationEnabledAsync();
        ViewBag.SummarizationProvider = await _configurationService.GetSummarizationProviderAsync();
        ViewBag.OpenAIApiKey = await _configurationService.GetOpenAIApiKeyAsync();
        ViewBag.LocalLlmUrl = await _configurationService.GetLocalLlmUrlAsync();
        ViewBag.LocalLlmModelName = await _configurationService.GetLocalLlmModelNameAsync();

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Settings(string domain, int port, string smtpHost, int smtpPort, string smtpUsername, string smtpPassword, bool smtpEnableSsl, bool internalRoutingEnabled, bool summarizationEnabled, string summarizationProvider, string openAIApiKey, string localLlmUrl, string localLlmModelName)
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

        // Save Summarization Settings
        await _configurationService.SetSummarizationEnabledAsync(summarizationEnabled);
        await _configurationService.SetSummarizationProviderAsync(summarizationProvider);
        await _configurationService.SetOpenAIApiKeyAsync(openAIApiKey);
        await _configurationService.SetLocalLlmUrlAsync(localLlmUrl);
        await _configurationService.SetLocalLlmModelNameAsync(localLlmModelName);

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
