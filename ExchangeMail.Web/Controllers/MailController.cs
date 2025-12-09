using ExchangeMail.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using MimeKit;
using MailKit.Net.Smtp;
using HtmlAgilityPack;

namespace ExchangeMail.Web.Controllers;

public class MailController : Controller
{
    private readonly IMailRepository _mailRepository;
    private readonly IUserRepository _userRepository;
    private readonly IConfigurationService _configurationService;
    private readonly ILogRepository _logRepository;
    private readonly ISafeSenderRepository _safeSenderRepository;
    private readonly IBlockListRepository _blockListRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IAiEmailService _aiEmailService;
    private readonly HtmlSanitizerService _htmlSanitizerService;

    private readonly PstImportService _pstImportService;
    private readonly ImportStatusService _importStatusService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public MailController(IMailRepository mailRepository, IUserRepository userRepository, IConfigurationService configurationService, ILogRepository logRepository, ISafeSenderRepository safeSenderRepository, HtmlSanitizerService htmlSanitizerService, PstImportService pstImportService, ImportStatusService importStatusService, IServiceScopeFactory serviceScopeFactory, IContactRepository contactRepository, IBlockListRepository blockListRepository, IAiEmailService aiEmailService)
    {
        _mailRepository = mailRepository;
        _userRepository = userRepository;
        _configurationService = configurationService;
        _logRepository = logRepository;
        _safeSenderRepository = safeSenderRepository;
        _htmlSanitizerService = htmlSanitizerService;
        _pstImportService = pstImportService;
        _importStatusService = importStatusService;
        _serviceScopeFactory = serviceScopeFactory;
        _contactRepository = contactRepository;
        _blockListRepository = blockListRepository;
        _aiEmailService = aiEmailService;
    }

    private string? GetCurrentUser() => User.Identity?.Name;
    private bool IsAdmin() => User.FindFirst("IsAdmin")?.Value == "True";

    private async Task<string> GetUserEmailAsync()
    {
        var username = GetCurrentUser();
        if (string.IsNullOrEmpty(username)) return string.Empty;

        var domain = await _configurationService.GetDomainAsync();
        var localPart = username.Contains("@") ? username.Split('@')[0] : username;

        Console.WriteLine($"[DEBUG] GetUserEmailAsync: Username '{username}', LocalPart '{localPart}', Configured Domain '{domain}'");
        return $"{localPart}@{domain}";
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, bool rememberMe)
    {
        var user = await _userRepository.ValidateUserAsync(username, password);
        if (user != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("Username", user.Username),
                new Claim("IsAdmin", user.IsAdmin.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe
            };

            if (rememberMe)
            {
                authProperties.ExpiresUtc = DateTime.UtcNow.AddDays(30);
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index");
        }
        ModelState.AddModelError("", "Invalid username or password");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmImport(string tempFilePath, string fileName, int totalMessages)
    {
        var username = GetCurrentUser();
        if (username == null) return RedirectToAction("Login");
        var userEmail = await GetUserEmailAsync();

        var jobId = Guid.NewGuid().ToString();
        _importStatusService.StartJob(jobId, totalMessages);

        // Run in background
        _ = Task.Run(async () =>
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var pstImportService = scope.ServiceProvider.GetRequiredService<PstImportService>();
                await pstImportService.ImportPstAsync(tempFilePath, userEmail, jobId);
            }
        });

        return RedirectToAction("ImportProgress", new { jobId = jobId });
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    public async Task<IActionResult> Index(string searchString, int page = 1, string folder = "Inbox", bool? focused = null)
    {
        var username = GetCurrentUser();
        if (username == null) return RedirectToAction("Login");

        var userEmail = await GetUserEmailAsync();
        int pageSize = 25;

        // Default to Focused if in Inbox and no preference specified
        if (folder == "Inbox" && focused == null) focused = true;

        var (messages, totalCount) = await _mailRepository.GetMessagesAsync(userEmail, searchString, page, pageSize, folder, focused);
        var folders = await _mailRepository.GetFoldersAsync(userEmail);
        var unreadCounts = await _mailRepository.GetUnreadCountsAsync(userEmail);

        ViewBag.UserEmail = userEmail;
        ViewBag.IsAdmin = IsAdmin();
        ViewBag.SearchString = searchString;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.Folder = folder;
        ViewBag.Focused = focused; // Pass to view for tabs
        ViewBag.Folders = folders;
        ViewBag.FolderUnreadCounts = unreadCounts;

        return View(messages);
    }

    [HttpGet]
    public async Task<IActionResult> GetLatestMessageSummary(string folder = "Inbox")
    {
        var username = GetCurrentUser();
        if (username == null) return Unauthorized();

        var userEmail = await GetUserEmailAsync();
        var summary = await _mailRepository.GetLatestMessageSummaryAsync(userEmail, folder);

        if (summary == null) return Json(new { id = (string?)null });

        if (summary == null) return Json(new { id = (string?)null });

        return Json(new { id = summary.Value.Id, from = summary.Value.From, subject = summary.Value.Subject });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        if (GetCurrentUser() == null) return Unauthorized();
        var userEmail = await GetUserEmailAsync();
        await _mailRepository.MarkAsReadAsync(id, userEmail);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsUnread(string id)
    {
        if (GetCurrentUser() == null) return Unauthorized();
        var userEmail = await GetUserEmailAsync();
        await _mailRepository.MarkAsUnreadAsync(id, userEmail);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> ToggleUrgent(string id)
    {
        if (GetCurrentUser() == null) return Unauthorized();
        var userEmail = await GetUserEmailAsync();

        var message = await _mailRepository.GetMessageAsync(id, userEmail);
        if (message == null) return NotFound();

        var currentLabels = message.Headers["X-Labels"] ?? "";
        var labelList = currentLabels.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(l => l.Trim())
                                     .ToList();

        if (labelList.Contains("Urgent", StringComparer.OrdinalIgnoreCase))
        {
            labelList.RemoveAll(l => l.Equals("Urgent", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            labelList.Add("Urgent");
        }

        var newLabels = string.Join(",", labelList);
        await _mailRepository.UpdateMessageLabelsAsync(id, userEmail, newLabels);

        return Json(new { success = true, isUrgent = labelList.Contains("Urgent", StringComparer.OrdinalIgnoreCase) });
    }

    [HttpPost]
    public async Task<IActionResult> Summarize(string id)
    {
        var username = GetCurrentUser();
        if (username == null) return Unauthorized();

        var userEmail = await GetUserEmailAsync();
        var message = await _mailRepository.GetMessageAsync(id, userEmail);
        if (message == null) return NotFound();

        // Prefer TextBody for summarization as it's cleaner, fallback to HtmlBody
        string content = message.TextBody ?? message.HtmlBody ?? "";

        // Simple Strip HTML if we fell back to HTML (very basic, for now relying on what we have)
        // Ideally we'd use HtmlAgilityPack to get text. 
        if (message.TextBody == null && !string.IsNullOrEmpty(message.HtmlBody))
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(message.HtmlBody);
            content = doc.DocumentNode.InnerText;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return Json(new { success = false, message = "Email has no content to summarize." });
        }

        // Limit content length to avoid token limits (rudimentary)
        if (content.Length > 10000)
        {
            content = content.Substring(0, 10000);
        }

        var summary = await _aiEmailService.SummarizeAsync(content);
        return Json(new { success = true, summary });
    }

    [HttpGet]
    public async Task<IActionResult> MessageList(string searchString, int page = 1, string folder = "Inbox", bool? focused = null)
    {
        var username = GetCurrentUser();
        if (username == null) return Unauthorized();

        var userEmail = await GetUserEmailAsync();
        int pageSize = 25;

        // Default to Focused if in Inbox and no preference specified
        if (folder == "Inbox" && focused == null) focused = true;

        var (messages, totalCount) = await _mailRepository.GetMessagesAsync(userEmail, searchString, page, pageSize, folder, focused);
        var unreadCounts = await _mailRepository.GetUnreadCountsAsync(userEmail);

        ViewBag.UserEmail = userEmail;
        ViewBag.IsAdmin = IsAdmin();
        ViewBag.SearchString = searchString;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.Folder = folder;
        ViewBag.Focused = focused;
        ViewBag.FolderUnreadCounts = unreadCounts;

        return PartialView("_MessageList", messages);
    }

    [HttpGet]
    public async Task<IActionResult> GetThreadMessages(string threadId)
    {
        if (GetCurrentUser() == null) return Unauthorized();
        var userEmail = await GetUserEmailAsync();

        var messages = await _mailRepository.GetThreadMessagesAsync(threadId, userEmail);
        return PartialView("_ThreadMessageList", messages);
    }

    public async Task<IActionResult> Details(string id, bool showBlocked = false)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login");

        var userEmail = await GetUserEmailAsync();
        var message = await _mailRepository.GetMessageAsync(id, userEmail);
        if (message == null)
        {
            return NotFound();
        }

        await _mailRepository.MarkAsReadAsync(id, userEmail);
        await EnsureLabelsAsync(message, id, userEmail);

        var senderEmail = message.From.Mailboxes.FirstOrDefault()?.Address;
        var isSafeSender = !string.IsNullOrEmpty(senderEmail) && await _safeSenderRepository.IsSafeSenderAsync(senderEmail);

        if (!isSafeSender && !showBlocked && !string.IsNullOrEmpty(message.HtmlBody))
        {
            var (sanitizedHtml, isBlocked) = _htmlSanitizerService.Sanitize(message.HtmlBody);
            if (isBlocked)
            {
                ViewBag.SanitizedHtml = sanitizedHtml;
                ViewBag.IsContentBlocked = true;
            }
        }

        ViewBag.IsSenderSafe = isSafeSender;
        ViewBag.ShowBlocked = showBlocked;

        return View(message);
    }

    [HttpGet]
    public async Task<IActionResult> MessagePartial(string id, bool showBlocked = false)
    {
        if (GetCurrentUser() == null) return Unauthorized();

        var userEmail = await GetUserEmailAsync();
        var message = await _mailRepository.GetMessageAsync(id, userEmail);
        if (message == null)
        {
            return NotFound();
        }

        await _mailRepository.MarkAsReadAsync(id, userEmail);
        await EnsureLabelsAsync(message, id, userEmail);

        var senderEmail = message.From.Mailboxes.FirstOrDefault()?.Address;
        var isSafeSender = !string.IsNullOrEmpty(senderEmail) && await _safeSenderRepository.IsSafeSenderAsync(senderEmail);

        if (!isSafeSender && !showBlocked && !string.IsNullOrEmpty(message.HtmlBody))
        {
            var (sanitizedHtml, isBlocked) = _htmlSanitizerService.Sanitize(message.HtmlBody);
            if (isBlocked)
            {
                ViewBag.SanitizedHtml = sanitizedHtml;
                ViewBag.IsContentBlocked = true;
            }
        }

        ViewBag.IsSenderSafe = isSafeSender;
        ViewBag.ShowBlocked = showBlocked;

        return PartialView("_ReadingPane", message);
    }

    private async Task EnsureLabelsAsync(MimeMessage message, string messageId, string userEmail)
    {
        // Check if labels are missing
        if (!message.Headers.Contains("X-Labels"))
        {
            // Check if Auto Labeling is enabled for this user
            var username = GetCurrentUser();
            if (username != null && await _userRepository.GetAutoLabelingAsync(username))
            {
                // Generate Labels
                string content = message.TextBody ?? message.HtmlBody ?? "";
                if (string.IsNullOrEmpty(message.TextBody) && !string.IsNullOrEmpty(message.HtmlBody))
                {
                    // Basic strip if possible? or just let AI handle it.
                    // The AI service handles raw text best usually.
                    var doc = new HtmlDocument();
                    doc.LoadHtml(message.HtmlBody);
                    content = doc.DocumentNode.InnerText;
                }

                if (!string.IsNullOrWhiteSpace(content))
                {
                    // Truncate if too long (simple check)
                    if (content.Length > 10000) content = content.Substring(0, 10000);

                    try
                    {
                        var labels = await _aiEmailService.GetLabelsAsync(content);
                        if (!string.IsNullOrEmpty(labels))
                        {
                            await _mailRepository.UpdateMessageLabelsAsync(messageId, userEmail, labels);
                            message.Headers.Add("X-Labels", labels);
                        }
                    }
                    catch (Exception ex)
                    {
                        await _logRepository.LogAsync("Error", "MailController", $"Failed to lazy-generate labels for {messageId}", ex);
                    }
                }
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> Body(string id, bool showBlocked = false)
    {
        if (GetCurrentUser() == null) return Unauthorized();

        var userEmail = await GetUserEmailAsync();
        var message = await _mailRepository.GetMessageAsync(id, userEmail);
        if (message == null) return NotFound();

        var senderEmail = message.From.Mailboxes.FirstOrDefault()?.Address;
        var isSafeSender = !string.IsNullOrEmpty(senderEmail) && await _safeSenderRepository.IsSafeSenderAsync(senderEmail);

        string htmlContent;

        if (!string.IsNullOrEmpty(message.HtmlBody))
        {
            if (!isSafeSender && !showBlocked)
            {
                var (sanitizedHtml, isBlocked) = _htmlSanitizerService.Sanitize(message.HtmlBody);
                htmlContent = sanitizedHtml;
            }
            else
            {
                htmlContent = message.HtmlBody;
            }
        }
        else
        {
            htmlContent = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: system-ui, -apple-system, sans-serif; padding: 1rem; color: #212529; }}
                        pre {{ white-space: pre-wrap; font-family: inherit; }}
                    </style>
                </head>
                <body>
                    <pre>{System.Net.WebUtility.HtmlEncode(message.TextBody ?? "")}</pre>
                </body>
                </html>";
        }

        return Content(htmlContent, "text/html");
    }

    [HttpPost]
    public async Task<IActionResult> TrustSender(string email, string returnUrl)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login");

        if (!string.IsNullOrEmpty(email))
        {
            await _safeSenderRepository.AddSafeSenderAsync(email);
        }

        if (!string.IsNullOrEmpty(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Delete(string id)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login");
        var userEmail = await GetUserEmailAsync();
        await _mailRepository.DeleteMessageAsync(id, userEmail);
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> PermanentDelete(string id)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login");
        var userEmail = await GetUserEmailAsync();
        await _mailRepository.PermanentDeleteMessageAsync(id, userEmail);
        return RedirectToAction("Index", new { folder = "Deleted Items" });
    }

    public async Task<IActionResult> EmptyTrash()
    {
        var username = GetCurrentUser();
        if (username == null) return RedirectToAction("Login");

        var userEmail = await GetUserEmailAsync();
        await _mailRepository.EmptyTrashAsync(userEmail);

        return RedirectToAction("Index", new { folder = "Deleted Items" });
    }

    public async Task<IActionResult> Reply(string id)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login");
        var userEmail = await GetUserEmailAsync();
        var message = await _mailRepository.GetMessageAsync(id, userEmail);
        if (message == null) return NotFound();

        var replyTo = message.From.Mailboxes.FirstOrDefault()?.Address ?? "";
        var subject = message.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? message.Subject : "Re: " + message.Subject;
        var body = $"\n\n> {message.TextBody?.Replace("\n", "\n> ")}";

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            ViewBag.From = await GetUserEmailAsync();
            ViewBag.To = replyTo;
            ViewBag.Subject = subject;
            ViewBag.Body = body;
            return PartialView("Compose");
        }

        return RedirectToAction("Compose", new { to = replyTo, subject = subject, body = body });
    }

    public async Task<IActionResult> ReplyAll(string id)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login");
        var userEmail = await GetUserEmailAsync();
        var message = await _mailRepository.GetMessageAsync(id, userEmail);
        if (message == null) return NotFound();

        var from = message.From.Mailboxes.FirstOrDefault()?.Address ?? "";
        var toList = message.To.Mailboxes.Select(m => m.Address);
        var ccList = message.Cc.Mailboxes.Select(m => m.Address);

        var allRecipients = new HashSet<string>();
        if (!string.IsNullOrEmpty(from)) allRecipients.Add(from);
        foreach (var t in toList) allRecipients.Add(t);
        foreach (var c in ccList) allRecipients.Add(c);

        var currentUserEmail = await GetUserEmailAsync();
        allRecipients.Remove(currentUserEmail);

        var to = string.Join(", ", allRecipients);

        var subject = message.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? message.Subject : "Re: " + message.Subject;
        var body = $"\n\n> {message.TextBody?.Replace("\n", "\n> ")}";

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            ViewBag.From = currentUserEmail;
            ViewBag.To = to;
            ViewBag.Subject = subject;
            ViewBag.Body = body;
            return PartialView("Compose");
        }

        return RedirectToAction("Compose", new { to = to, subject = subject, body = body });
    }

    public async Task<IActionResult> Forward(string id)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login");
        var userEmail = await GetUserEmailAsync();
        var message = await _mailRepository.GetMessageAsync(id, userEmail);
        if (message == null) return NotFound();

        var subject = message.Subject.StartsWith("Fwd:", StringComparison.OrdinalIgnoreCase) ? message.Subject : "Fwd: " + message.Subject;
        var body = $"\n\n---------- Forwarded message ----------\nFrom: {message.From}\nDate: {message.Date}\nSubject: {message.Subject}\nTo: {message.To}\n\n{message.TextBody}";

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            ViewBag.From = await GetUserEmailAsync();
            ViewBag.To = ""; // Forward usually doesn't have a To address pre-filled
            ViewBag.Subject = subject;
            ViewBag.Body = body;
            return PartialView("Compose");
        }

        return RedirectToAction("Compose", new { subject = subject, body = body });
    }

    public async Task<IActionResult> Download(string id)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login");
        var userEmail = await GetUserEmailAsync();
        var message = await _mailRepository.GetMessageAsync(id, userEmail);
        if (message == null) return NotFound();

        using var stream = new MemoryStream();
        await message.WriteToAsync(stream);
        return File(stream.ToArray(), "message/rfc822", $"{id}.eml");
    }

    public async Task<IActionResult> DownloadAttachment(string id, string fileName)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login");
        var userEmail = await GetUserEmailAsync();
        var message = await _mailRepository.GetMessageAsync(id, userEmail);
        if (message == null) return NotFound();

        var attachment = message.Attachments.FirstOrDefault(a => a.ContentDisposition?.FileName == fileName || a.ContentType.Name == fileName);
        if (attachment is MimePart mimePart)
        {
            using var stream = new MemoryStream();
            await mimePart.Content.DecodeToAsync(stream);
            return File(stream.ToArray(), mimePart.ContentType.MimeType, fileName);
        }

        return NotFound();
    }

    public async Task<IActionResult> Compose(string? to, string? subject, string? body, string? id)
    {
        var username = GetCurrentUser();
        if (username == null) return RedirectToAction("Login");

        ViewBag.From = await GetUserEmailAsync();
        ViewBag.To = to;
        ViewBag.Subject = subject;
        ViewBag.Body = body;
        ViewBag.DraftId = id;

        if (!string.IsNullOrEmpty(id))
        {
            var userEmail = await GetUserEmailAsync();
            var message = await _mailRepository.GetMessageAsync(id, userEmail);
            if (message != null)
            {
                ViewBag.To = message.To.ToString();
                ViewBag.Subject = message.Subject;
                ViewBag.Body = message.HtmlBody ?? message.TextBody;

                // Handle Cc/Bcc if needed, but for now just basic fields
                ViewBag.Cc = message.Cc.ToString();
                ViewBag.Bcc = message.Bcc.ToString();
            }
        }
        else if (string.IsNullOrEmpty(body))
        {
            // New email, append signature
            var signature = await _userRepository.GetSignatureAsync(username);
            if (!string.IsNullOrEmpty(signature))
            {
                ViewBag.Body = "<br><br>" + signature;
            }
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView();
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Send(string to, string? cc, string? bcc, string subject, string body, List<IFormFile> attachments)
    {
        Console.WriteLine($"[DEBUG] Entering Send method. To: {to}, Cc: {cc}, Bcc: {bcc}, Subject: {subject}, Attachments: {attachments?.Count ?? 0}");
        var username = GetCurrentUser();
        if (username == null)
        {
            Console.WriteLine("[DEBUG] User not logged in.");
            return RedirectToAction("Login");
        }

        var fromAddress = await GetUserEmailAsync();
        Console.WriteLine($"[DEBUG] From: {fromAddress}");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(username, fromAddress));
        message.To.Add(new MailboxAddress("", to));

        if (!string.IsNullOrEmpty(cc))
        {
            foreach (var address in cc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.Cc.Add(new MailboxAddress("", address.Trim()));
            }
        }

        if (!string.IsNullOrEmpty(bcc))
        {
            foreach (var address in bcc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.Bcc.Add(new MailboxAddress("", address.Trim()));
            }
        }

        message.Subject = subject;

        var builder = new BodyBuilder();

        // Check if the user pasted raw HTML source code into the editor
        // Quill sends this as escaped HTML wrapped in tags (e.g. <p>&lt;!DOCTYPE html&gt;...</p>)
        var doc = new HtmlDocument();
        doc.LoadHtml(body);
        var text = HtmlEntity.DeEntitize(doc.DocumentNode.InnerText).Trim();

        if (text.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
        {
            // User intended to send raw HTML
            builder.HtmlBody = text;

            // Try to generate a plain text version from the raw HTML
            var plainTextDoc = new HtmlDocument();
            plainTextDoc.LoadHtml(text);
            builder.TextBody = plainTextDoc.DocumentNode.InnerText;
        }
        else
        {
            // Normal rich text email
            builder.HtmlBody = body;
            builder.TextBody = text;
        }

        // Log attachment count
        await _logRepository.LogAsync("Info", "Debug", $"Sending email to {to}. Attachments: {attachments?.Count ?? 0}");

        if (attachments != null && attachments.Any())
        {
            foreach (var attachment in attachments)
            {
                if (attachment.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await attachment.CopyToAsync(ms);
                        builder.Attachments.Add(attachment.FileName, ms.ToArray(), ContentType.Parse(attachment.ContentType));
                    }
                }
            }
        }

        message.Body = builder.ToMessageBody();

        bool isAjax = false;
        if (Request.Headers.TryGetValue("X-Requested-With", out var headerValue) &&
            string.Equals(headerValue, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
        {
            isAjax = true;
        }

        try
        {
            Console.WriteLine("[DEBUG] Creating SmtpClient");
            using (var client = new SmtpClient())
            {
                var smtpHost = await _configurationService.GetSmtpHostAsync();
                var internalRoutingEnabled = await _configurationService.GetInternalRoutingEnabledAsync();
                var domain = await _configurationService.GetDomainAsync();

                // Determine if we should route internally
                bool routeInternally = false;

                if (internalRoutingEnabled)
                {
                    if (to.EndsWith("@" + domain, StringComparison.OrdinalIgnoreCase))
                    {
                        routeInternally = true;
                        Console.WriteLine($"[DEBUG] Smart Internal Routing: Recipient {to} is local. Routing internally.");
                    }
                }

                if (string.IsNullOrEmpty(smtpHost))
                {
                    routeInternally = true;
                }

                Console.WriteLine($"[DEBUG] SMTP Host: '{smtpHost}', Route Internally: {routeInternally}");

                if (!routeInternally)
                {
                    // Upstream Sending
                    var smtpPort = await _configurationService.GetSmtpPortAsync();
                    var enableSsl = await _configurationService.GetSmtpEnableSslAsync();
                    var smtpUsername = await _configurationService.GetSmtpUsernameAsync();
                    var smtpPassword = await _configurationService.GetSmtpPasswordAsync();

                    Console.WriteLine($"[DEBUG] Connecting to {smtpHost}:{smtpPort} (SSL: {enableSsl})");

                    var socketOptions = MailKit.Security.SecureSocketOptions.Auto;
                    if (enableSsl)
                    {
                        socketOptions = smtpPort == 465
                            ? MailKit.Security.SecureSocketOptions.SslOnConnect
                            : MailKit.Security.SecureSocketOptions.StartTls;
                    }

                    await client.ConnectAsync(smtpHost, smtpPort, socketOptions);
                    Console.WriteLine("[DEBUG] Connected");

                    if (!string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword))
                    {
                        Console.WriteLine("[DEBUG] Authenticating");
                        await client.AuthenticateAsync(smtpUsername, smtpPassword);
                        Console.WriteLine("[DEBUG] Authenticated");
                    }

                    Console.WriteLine("[DEBUG] Sending message");
                    await client.SendAsync(message);
                    Console.WriteLine("[DEBUG] Message sent");
                    await client.DisconnectAsync(true);

                    await _logRepository.LogAsync("Info", "SMTP", $"Successfully sent email to {to} via {smtpHost}");
                }
                else
                {
                    // Internal Loopback
                    var port = await _configurationService.GetPortAsync();
                    Console.WriteLine($"[DEBUG] Connecting to localhost:{port}");
                    await client.ConnectAsync("localhost", port, false);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);

                    await _logRepository.LogAsync("Info", "SMTP", $"Successfully sent email to {to} via Internal Loopback");
                }
            }

            await _mailRepository.SaveMessageAsync(message, "Sent Items", username);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception: {ex.Message}");
            Console.WriteLine($"[DEBUG] StackTrace: {ex.StackTrace}");
            await _logRepository.LogAsync("Error", "SMTP", $"Failed to send email to {to}", ex);

            if (isAjax)
            {
                return StatusCode(500, "Failed to send email: " + ex.Message);
            }

            ViewBag.To = to;
            ViewBag.Cc = cc;
            ViewBag.Bcc = bcc;
            ViewBag.Subject = subject;
            ViewBag.Body = body;
            ViewBag.ErrorMessage = $"Failed to send email: {ex.Message}";

            return View("Compose");
        }

        Console.WriteLine("[DEBUG] Returning result");



        Console.WriteLine($"[DEBUG] Is Ajax: {isAjax}");

        if (isAjax)
        {
            return Ok();
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> SaveDraft(string to, string? cc, string? bcc, string subject, string body, List<IFormFile> attachments, string? id)
    {
        var username = GetCurrentUser();
        if (username == null) return RedirectToAction("Login");

        var fromAddress = await GetUserEmailAsync();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(username, fromAddress));

        if (!string.IsNullOrEmpty(to))
        {
            foreach (var address in to.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.To.Add(new MailboxAddress("", address.Trim()));
            }
        }

        if (!string.IsNullOrEmpty(cc))
        {
            foreach (var address in cc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.Cc.Add(new MailboxAddress("", address.Trim()));
            }
        }

        if (!string.IsNullOrEmpty(bcc))
        {
            foreach (var address in bcc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.Bcc.Add(new MailboxAddress("", address.Trim()));
            }
        }

        message.Subject = subject ?? "(No Subject)";

        var builder = new BodyBuilder();

        // Handle body content similar to Send method
        if (!string.IsNullOrEmpty(body))
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(body);
            var text = HtmlEntity.DeEntitize(doc.DocumentNode.InnerText).Trim();

            if (text.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            {
                builder.HtmlBody = text;
                var plainTextDoc = new HtmlDocument();
                plainTextDoc.LoadHtml(text);
                builder.TextBody = plainTextDoc.DocumentNode.InnerText;
            }
            else
            {
                builder.HtmlBody = body;
                builder.TextBody = text;
            }
        }

        if (attachments != null && attachments.Any())
        {
            foreach (var attachment in attachments)
            {
                if (attachment.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await attachment.CopyToAsync(ms);
                        builder.Attachments.Add(attachment.FileName, ms.ToArray(), ContentType.Parse(attachment.ContentType));
                    }
                }
            }
        }

        message.Body = builder.ToMessageBody();

        // Ensure "Drafts" folder exists
        await _mailRepository.CreateFolderAsync("Drafts", fromAddress);

        if (!string.IsNullOrEmpty(id))
        {
            await _mailRepository.UpdateMessageAsync(id, message);
        }
        else
        {
            // Save to Drafts
            await _mailRepository.SaveMessageAsync(message, "Drafts", fromAddress);
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Ok();
        }

        return RedirectToAction("Index", new { folder = "Drafts" });
    }

    [HttpPost]
    public async Task<IActionResult> CreateFolder(string name)
    {
        var userEmail = await GetUserEmailAsync();
        if (string.IsNullOrEmpty(userEmail)) return Unauthorized();

        await _mailRepository.CreateFolderAsync(name, userEmail);
        return RedirectToAction("Index", new { folder = name });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead(string folder)
    {
        var userEmail = await GetUserEmailAsync();
        if (string.IsNullOrEmpty(userEmail)) return Unauthorized();

        await _mailRepository.MarkAllAsReadAsync(userEmail, folder);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteFolder(string name)
    {
        var userEmail = await GetUserEmailAsync();
        if (string.IsNullOrEmpty(userEmail)) return Unauthorized();

        await _mailRepository.DeleteFolderAsync(name, userEmail);
        return RedirectToAction("Index", new { folder = "Inbox" });
    }



    [HttpPost]
    public async Task<IActionResult> MoveMessage(string id, string folder)
    {
        if (GetCurrentUser() == null) return Unauthorized();

        var userEmail = await GetUserEmailAsync();
        await _mailRepository.MoveMessageAsync(id, folder, userEmail);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> CopyMessage(string id, string folder)
    {
        if (GetCurrentUser() == null) return Unauthorized();

        var userEmail = await GetUserEmailAsync();
        await _mailRepository.CopyMessageAsync(id, folder, userEmail);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsJunk(string id)
    {
        if (GetCurrentUser() == null) return Unauthorized();

        var userEmail = await GetUserEmailAsync();
        var message = await _mailRepository.GetMessageAsync(id, userEmail);
        if (message != null)
        {
            var sender = message.From.Mailboxes.FirstOrDefault()?.Address;
            if (!string.IsNullOrEmpty(sender))
            {
                await _blockListRepository.AddBlockedSenderAsync(sender);
                await _safeSenderRepository.RemoveSafeSenderAsync(sender);
            }

            await _mailRepository.MoveMessageAsync(id, "Junk Email", userEmail);
        }

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsNotJunk(string id)
    {
        if (GetCurrentUser() == null) return Unauthorized();

        var userEmail = await GetUserEmailAsync();
        var message = await _mailRepository.GetMessageAsync(id, userEmail);
        if (message != null)
        {
            var sender = message.From.Mailboxes.FirstOrDefault()?.Address;
            if (!string.IsNullOrEmpty(sender))
            {
                await _blockListRepository.RemoveBlockedSenderAsync(sender);
                await _safeSenderRepository.AddSafeSenderAsync(sender);
            }

            await _mailRepository.MoveMessageAsync(id, "Inbox", userEmail);
        }

        return Ok();
    }
    [HttpGet]
    public IActionResult ImportPst()
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login");
        return View();
    }

    [HttpPost]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> ImportPst(IFormFile pstFile)
    {
        var username = GetCurrentUser();
        if (username == null) return RedirectToAction("Login");

        if (pstFile != null && pstFile.Length > 0)
        {
            var (totalMessages, tempFilePath, folderDebugInfo) = await _pstImportService.AnalyzePstAsync(pstFile.OpenReadStream());

            ViewBag.TotalMessages = totalMessages;
            ViewBag.TempFilePath = tempFilePath;
            ViewBag.FileName = pstFile.FileName;
            ViewBag.FolderDebugInfo = folderDebugInfo;

            return View("ConfirmImport");
        }

        ModelState.AddModelError("", "Please select a file.");
        return View();
    }



    [HttpGet]
    public IActionResult ImportProgress(string jobId)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login");
        ViewBag.JobId = jobId;
        return View();
    }

    [HttpGet]
    public IActionResult GetImportStatus(string jobId)
    {
        if (GetCurrentUser() == null) return Unauthorized();
        var status = _importStatusService.GetStatus(jobId);
        if (status == null) return NotFound();
        return Json(status);
    }

    [HttpGet]
    public async Task<IActionResult> SearchContacts(string query)
    {
        if (GetCurrentUser() == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(query)) return Json(new List<object>());

        var contacts = await _contactRepository.SearchContactsAsync(query);
        var users = await _userRepository.GetAllUsersAsync();

        var matchingUsers = users.Where(u => u.Username.Contains(query, StringComparison.OrdinalIgnoreCase));

        var results = new List<object>();

        foreach (var contact in contacts)
        {
            results.Add(new { name = contact.Name, email = contact.Email });
        }

        var domain = await _configurationService.GetDomainAsync();
        foreach (var user in matchingUsers)
        {
            // Avoid duplicates if user is also in contacts (unlikely but possible)
            var email = user.Username.Contains("@") ? user.Username : $"{user.Username}@{domain}";
            if (!results.Any(r => ((dynamic)r).email == email))
            {
                results.Add(new { name = user.Username, email = email });
            }
        }

        return Json(results);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateDraft([FromBody] DraftGenerationRequest request)
    {
        try
        {
            var username = GetCurrentUser();
            if (username == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request?.Prompt))
            {
                return Json(new { success = false, message = "Prompt is required." });
            }

            await _logRepository.LogAsync("Info", "AI", $"Generating draft for user {username} with prompt: {request.Prompt}");

            var draft = await _aiEmailService.GenerateDraftAsync(request.Prompt);
            return Json(new { success = true, draft });
        }
        catch (Exception ex)
        {
            await _logRepository.LogAsync("Error", "AI", $"Error generating draft: {ex.Message}", ex);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    public class DraftGenerationRequest
    {
        public string? Prompt { get; set; }
    }
}
