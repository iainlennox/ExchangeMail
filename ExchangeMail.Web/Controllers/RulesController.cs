using ExchangeMail.Core.Data.Entities;
using ExchangeMail.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExchangeMail.Web.Controllers;

public class RulesController : Controller
{
    private readonly IMailRuleRepository _ruleRepository;
    private readonly IMailRepository _mailRepository;

    public RulesController(IMailRuleRepository ruleRepository, IMailRepository mailRepository)
    {
        _ruleRepository = ruleRepository;
        _mailRepository = mailRepository;
    }

    public async Task<IActionResult> Index()
    {
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Mail");

        var rules = await _ruleRepository.GetRulesAsync(userEmail);
        var folders = await _mailRepository.GetFoldersAsync(userEmail);

        // Add standard folders
        var allFolders = new List<string> { "Inbox", "Junk Email", "Deleted Items" };
        allFolders.AddRange(folders);

        ViewBag.Folders = allFolders;

        return View(rules);
    }

    [HttpGet]
    public async Task<IActionResult> Editor(int? id)
    {
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Mail");

        // Populate folders for dropdowns
        var folders = await _mailRepository.GetFoldersAsync(userEmail);
        var allFolders = new List<string> { "Inbox", "Junk Email", "Deleted Items" };
        allFolders.AddRange(folders);
        ViewBag.Folders = allFolders;

        if (id.HasValue && id > 0)
        {
            var rules = await _ruleRepository.GetRulesAsync(userEmail);
            var rule = rules.FirstOrDefault(r => r.Id == id.Value);
            if (rule == null) return NotFound();
            return View(rule);
        }

        // New Rule Default
        return View(new MailRuleEntity
        {
            UserEmail = userEmail,
            Name = "New Rule",
            Conditions = new List<MailRuleConditionEntity>(),
            Actions = new List<MailRuleActionEntity>()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save(MailRuleEntity rule)
    {
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Mail");

        rule.UserEmail = userEmail;

        // Ensure Conditions and Actions are initialized
        rule.Conditions ??= new List<MailRuleConditionEntity>();
        rule.Actions ??= new List<MailRuleActionEntity>();

        // Ensure folders exist for MoveToFolder actions
        foreach (var action in rule.Actions.Where(a => a.ActionType == RuleActionType.MoveToFolder && !string.IsNullOrEmpty(a.TargetValue)))
        {
            await _mailRepository.CreateFolderAsync(action.TargetValue!, userEmail);
        }

        if (rule.Id == 0)
        {
            await _ruleRepository.AddRuleAsync(rule);
        }
        else
        {
            await _ruleRepository.UpdateRuleAsync(rule);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _ruleRepository.DeleteRuleAsync(id);
        return RedirectToAction("Index");
    }
}
