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
    private readonly IMailRuleService _mailRuleService;

    public AdminController(IUserRepository userRepository, IConfigurationService configurationService, IMailRepository mailRepository, ILogRepository logRepository, ITaskRepository taskRepository, ICalendarRepository calendarRepository, IMailRuleService mailRuleService)
    {
        _userRepository = userRepository;
        _configurationService = configurationService;
        _mailRepository = mailRepository;
        _logRepository = logRepository;
        _taskRepository = taskRepository;
        _calendarRepository = calendarRepository;
        _mailRuleService = mailRuleService;
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
                var userStates = new List<(string UserEmail, string? Folder, string? Labels)> { (username, null, labels) };
                await _mailRepository.SaveMessageWithUserStatesAsync(mimeMessage, userStates);
            }

            // 4. Threaded Conversation (Project Phoenix)
            var threadId = Guid.NewGuid().ToString();
            var msg1Id = Guid.NewGuid().ToString() + "@demo.local";
            var msg2Id = Guid.NewGuid().ToString() + "@demo.local";
            var msg3Id = Guid.NewGuid().ToString() + "@demo.local";

            // Msg 1
            var tMsg1 = new MimeMessage();
            tMsg1.From.Add(new MailboxAddress("Project Lead", "lead@company.com"));
            tMsg1.To.Add(new MailboxAddress(username, username));
            tMsg1.Subject = "Project Phoenix Kickoff";
            tMsg1.Body = new TextPart("plain") { Text = "Hi Team,\n\nWe are starting Project Phoenix properly today. Please reveiw the attached docs." };
            tMsg1.Date = DateTimeOffset.Now.AddDays(-2);
            tMsg1.MessageId = msg1Id;
            await _mailRepository.SaveMessageWithUserStatesAsync(tMsg1, new[] { (username, (string?)null, (string?)null) });

            // Msg 2 (Reply)
            var tMsg2 = new MimeMessage();
            tMsg2.From.Add(new MailboxAddress("Developer", "dev@company.com"));
            tMsg2.To.Add(new MailboxAddress("Project Lead", "lead@company.com"));
            tMsg2.Cc.Add(new MailboxAddress(username, username));
            tMsg2.Subject = "Re: Project Phoenix Kickoff";
            tMsg2.Body = new TextPart("plain") { Text = "Docs look good. I will start the repo setup." };
            tMsg2.Date = DateTimeOffset.Now.AddDays(-1);
            tMsg2.MessageId = msg2Id;
            tMsg2.InReplyTo = msg1Id;
            tMsg2.References.Add(msg1Id);
            await _mailRepository.SaveMessageWithUserStatesAsync(tMsg2, new[] { (username, (string?)null, (string?)null) });

            // Msg 3 (Reply All)
            var tMsg3 = new MimeMessage();
            tMsg3.From.Add(new MailboxAddress("Project Lead", "lead@company.com"));
            tMsg3.To.Add(new MailboxAddress("Developer", "dev@company.com"));
            tMsg3.Cc.Add(new MailboxAddress(username, username));
            tMsg3.Subject = "Re: Project Phoenix Kickoff";
            tMsg3.Body = new TextPart("plain") { Text = "Great. Let's sync tomorrow at 10am." };
            tMsg3.Date = DateTimeOffset.Now.AddHours(-2);
            tMsg3.MessageId = msg3Id;
            tMsg3.InReplyTo = msg2Id;
            tMsg3.References.Add(msg1Id);
            tMsg3.References.Add(msg2Id);
            await _mailRepository.SaveMessageWithUserStatesAsync(tMsg3, new[] { (username, (string?)null, (string?)null) });

            // 5. Support Ticket Thread
            var ticketThreadId = Guid.NewGuid().ToString();
            var ticketMsg1Id = Guid.NewGuid().ToString() + "@support.local";
            var ticketMsg2Id = Guid.NewGuid().ToString() + "@support.local";

            var tMsgTicket1 = new MimeMessage();
            tMsgTicket1.From.Add(new MailboxAddress("Client Services", "support@vendor.com"));
            tMsgTicket1.To.Add(new MailboxAddress(username, username));
            tMsgTicket1.Subject = "Ticket #9281: Access Issue";
            tMsgTicket1.Body = new TextPart("plain") { Text = "Hello,\n\nWe have received your request regarding login issues. Can you please confirm your browser version?" };
            tMsgTicket1.Date = DateTimeOffset.Now.AddDays(-3);
            tMsgTicket1.MessageId = ticketMsg1Id;
            await _mailRepository.SaveMessageWithUserStatesAsync(tMsgTicket1, new[] { (username, (string?)null, (string?)"Work") });

            var tMsgTicket2 = new MimeMessage();
            tMsgTicket2.From.Add(new MailboxAddress("Client Services", "support@vendor.com"));
            tMsgTicket2.To.Add(new MailboxAddress(username, username));
            tMsgTicket2.Subject = "Re: Ticket #9281: Access Issue";
            tMsgTicket2.Body = new TextPart("plain") { Text = "Thanks for the info. We have applied a fix. Please try again." };
            tMsgTicket2.Date = DateTimeOffset.Now.AddDays(-1);
            tMsgTicket2.MessageId = ticketMsg2Id;
            tMsgTicket2.InReplyTo = ticketMsg1Id;
            tMsgTicket2.References.Add(ticketMsg1Id);
            await _mailRepository.SaveMessageWithUserStatesAsync(tMsgTicket2, new[] { (username, (string?)null, (string?)"Work") });


            // 6. Team Lunch Thread
            var lunchMsg1Id = Guid.NewGuid().ToString() + "@fun.local";
            var lunchMsg2Id = Guid.NewGuid().ToString() + "@fun.local";
            var lunchMsg3Id = Guid.NewGuid().ToString() + "@fun.local";
            var lunchMsg4Id = Guid.NewGuid().ToString() + "@fun.local";

            var tMsgLunch1 = new MimeMessage();
            tMsgLunch1.From.Add(new MailboxAddress("Sarah", "sarah@company.com"));
            tMsgLunch1.To.Add(new MailboxAddress("Team", "team@company.com"));
            tMsgLunch1.Subject = "Team Lunch Friday?";
            tMsgLunch1.Body = new TextPart("plain") { Text = "Hey everyone,\n\nThinking of going to that new italian place on Friday. Thoughts?" };
            tMsgLunch1.Date = DateTimeOffset.Now.AddHours(-5);
            tMsgLunch1.MessageId = lunchMsg1Id;
            await _mailRepository.SaveMessageWithUserStatesAsync(tMsgLunch1, new[] { (username, (string?)null, (string?)"Social") });

            var tMsgLunch2 = new MimeMessage();
            tMsgLunch2.From.Add(new MailboxAddress("Mike", "mike@company.com"));
            tMsgLunch2.To.Add(new MailboxAddress("Sarah", "sarah@company.com"));
            tMsgLunch2.Cc.Add(new MailboxAddress("Team", "team@company.com"));
            tMsgLunch2.Subject = "Re: Team Lunch Friday?";
            tMsgLunch2.Body = new TextPart("plain") { Text = "I'm in! 🍕" };
            tMsgLunch2.Date = DateTimeOffset.Now.AddHours(-4);
            tMsgLunch2.MessageId = lunchMsg2Id;
            tMsgLunch2.InReplyTo = lunchMsg1Id;
            tMsgLunch2.References.Add(lunchMsg1Id);
            await _mailRepository.SaveMessageWithUserStatesAsync(tMsgLunch2, new[] { (username, (string?)null, (string?)"Social") });

            var tMsgLunch3 = new MimeMessage();
            tMsgLunch3.From.Add(new MailboxAddress("John", "john@company.com"));
            tMsgLunch3.To.Add(new MailboxAddress("Sarah", "sarah@company.com"));
            tMsgLunch3.Cc.Add(new MailboxAddress("Team", "team@company.com"));
            tMsgLunch3.Subject = "Re: Team Lunch Friday?";
            tMsgLunch3.Body = new TextPart("plain") { Text = "Can't make it, deadline looming. Enjoy!" };
            tMsgLunch3.Date = DateTimeOffset.Now.AddHours(-3);
            tMsgLunch3.MessageId = lunchMsg3Id;
            tMsgLunch3.InReplyTo = lunchMsg2Id;
            tMsgLunch3.References.Add(lunchMsg1Id);
            tMsgLunch3.References.Add(lunchMsg2Id);
            await _mailRepository.SaveMessageWithUserStatesAsync(tMsgLunch3, new[] { (username, (string?)null, (string?)"Social") });

            var tMsgLunch4 = new MimeMessage();
            tMsgLunch4.From.Add(new MailboxAddress("Sarah", "sarah@company.com"));
            tMsgLunch4.To.Add(new MailboxAddress("Team", "team@company.com"));
            tMsgLunch4.Subject = "Re: Team Lunch Friday?";
            tMsgLunch4.Body = new TextPart("plain") { Text = "Ok, table booked for 6 people at 12:30." };
            tMsgLunch4.Date = DateTimeOffset.Now.AddHours(-1);
            TempData["SuccessMessage"] = $"Demo data generated successfully for user {username}.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error generating demo data: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> GenerateRuleTestEmails(string username)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Mail");

        try
        {
            var today = DateTime.Today;

            // Define Test Scenarios mapped to System Rules
            var testCases = new List<(string Subject, string From, string Body, string? ExpectedFolder, Dictionary<string, string> Headers)>
            {
                // 1. Security
                ("Suspicious sign in attempt blocked", "security@google.com", "We blocked a sign-in attempt.", "Inbox", new Dictionary<string, string>()), // Should be Flagged only

                // 2. Finance
                ("Your Bank Statement", "statements@bank.com", "Your statement is ready.", "Finance", new Dictionary<string, string>()),
                
                // 3. Shopping
                ("Order confirmation #12345", "orders@amazon.com", "Thank you for your order.", "Shopping", new Dictionary<string, string>()),

                // 4. Social
                ("New friend request", "noreply@facebookmail.com", "You have a new friend request.", "Social", new Dictionary<string, string>()),
                
                // 5. Marketing
                ("Huge Sale! 50% Off", "offers@bestbuy.com", "Don't miss out.", "Marketing", new Dictionary<string, string>()),

                // 6. Mailing Lists
                ("Weekly Newsletter", "newsletter@techcrunch.com", "This week in tech.", "Mailing Lists", new Dictionary<string, string> { { "List-Id", "techcrunch-weekly" } }),

                // 7. Notifications (Catch-all)
                ("Your ticket has been updated", "noreply@jira.atlassian.net", "Ticket KEY-123 updated.", "Notifications", new Dictionary<string, string>())
            };

            foreach (var testCase in testCases)
            {
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress("Test Sender", testCase.From));
                mimeMessage.To.Add(new MailboxAddress(username, username));
                mimeMessage.Subject = testCase.Subject;
                mimeMessage.Body = new TextPart("plain") { Text = testCase.Body };
                mimeMessage.Date = DateTimeOffset.Now;

                foreach (var header in testCase.Headers)
                {
                    mimeMessage.Headers.Add(header.Key, header.Value);
                }

                // Apply Rules
                var ruleResult = await _mailRuleService.ApplyRulesAsync(mimeMessage, username);

                string? folder = ruleResult.TargetFolder; // May be null if only Flagged (Security) or no match
                string? labels = ruleResult.Labels.Any() ? string.Join(",", ruleResult.Labels) : null;

                // Ensure folder exists if rule set it
                if (!string.IsNullOrEmpty(folder))
                {
                    await _mailRepository.CreateFolderAsync(folder, username);
                }

                var userStates = new List<(string UserEmail, string? Folder, string? Labels)> { (username, folder, labels) };
                await _mailRepository.SaveMessageWithUserStatesAsync(mimeMessage, userStates);
            }

            TempData["SuccessMessage"] = $"Rule test emails generated successfully for user {username}. Check your folders.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error generating rule test data: {ex.Message}";
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
            var demoEmailSubjects = new[] { "Urgent: Server Update Required", "Welcome to the team!", "Meeting Notes", "Project Phoenix Kickoff", "Re: Project Phoenix Kickoff" };

            foreach (var msg in userMessages.Where(m => demoEmailSubjects.Contains(m.Subject)))
            {
                var idHeader = msg.Headers["X-Db-Id"];
                if (!string.IsNullOrEmpty(idHeader))
                {
                    await _mailRepository.PermanentDeleteMessageAsync(idHeader, username);
                }
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error clearing demo data: {ex.Message}";
        }

        TempData["SuccessMessage"] = $"Demo data cleared successfully for user {username}.";
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
