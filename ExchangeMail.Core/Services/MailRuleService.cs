using MimeKit;
using ExchangeMail.Core.Data.Entities;

namespace ExchangeMail.Core.Services;

public class MailRuleService : IMailRuleService
{
    private readonly IMailRuleRepository _repository;
    private readonly IMailRepository _mailRepository;
    private readonly IRuleMatcher _ruleMatcher;

    public MailRuleService(IMailRuleRepository repository, IMailRepository mailRepository, IRuleMatcher ruleMatcher)
    {
        _repository = repository;
        _mailRepository = mailRepository;
        _ruleMatcher = ruleMatcher;
    }

    public async Task<RuleResult> ApplyRulesAsync(MimeMessage message, string userEmail)
    {
        var rules = (await _repository.GetRulesAsync(userEmail)).ToList();

        // Seed default rules if none exist
        if (!rules.Any())
        {
            var systemRules = SystemRulesSeeder.GetSystemRules(userEmail);
            foreach (var rule in systemRules)
            {
                await _repository.AddRuleAsync(rule);

                // Ensure folders exist for MoveToFolder actions
                foreach (var action in rule.Actions.Where(a => a.ActionType == RuleActionType.MoveToFolder && !string.IsNullOrEmpty(a.TargetValue)))
                {
                    await _mailRepository.CreateFolderAsync(action.TargetValue!, userEmail);
                }
            }
            rules = systemRules;
        }

        var actions = _ruleMatcher.Match(message, rules);
        var result = new RuleResult();

        foreach (var action in actions)
        {
            switch (action.ActionType)
            {
                case RuleActionType.MoveToFolder:
                    result.TargetFolder = action.TargetValue;
                    break;
                case RuleActionType.MarkAsRead:
                    result.MarkAsRead = true;
                    break;
                case RuleActionType.AddLabel:
                    if (!string.IsNullOrEmpty(action.TargetValue))
                    {
                        result.Labels.Add(action.TargetValue);
                    }
                    break;
                case RuleActionType.Flag:
                    result.Flag = true;
                    break;
                case RuleActionType.Delete:
                    result.Delete = true;
                    break;
                case RuleActionType.StopProcessing:
                    result.StopProcessing = true;
                    break;
            }
        }

        return result;
    }
}
