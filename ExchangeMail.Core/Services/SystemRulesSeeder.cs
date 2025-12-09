using ExchangeMail.Core.Data.Entities;

namespace ExchangeMail.Core.Services;

public static class SystemRulesSeeder
{
    public static List<MailRuleEntity> GetSystemRules(string userEmail)
    {
        var rules = new List<MailRuleEntity>();
        int priority = 10;

        // 1. Security Alerts (Keep top, allows highlighting)
        rules.Add(new MailRuleEntity
        {
            UserEmail = userEmail,
            Name = "Security Alerts",
            Priority = priority++,
            MatchMode = RuleMatchMode.Any,
            IsGlobal = true,
            StopProcessing = false, // Highlight but allow further processing (e.g. moving)
            Conditions = new List<MailRuleConditionEntity>
            {
                new() { Field = RuleConditionField.Subject, Operator = RuleConditionOperator.Contains, Value = "Verification code" },
                new() { Field = RuleConditionField.Subject, Operator = RuleConditionOperator.Contains, Value = "Password reset" },
                new() { Field = RuleConditionField.Subject, Operator = RuleConditionOperator.Contains, Value = "Suspicious sign in" }
            },
            Actions = new List<MailRuleActionEntity>
            {
                new() { ActionType = RuleActionType.Flag },
                new() { ActionType = RuleActionType.AddLabel, TargetValue = "Security" }
            }
        });

        // 2. Finance
        rules.Add(new MailRuleEntity
        {
            UserEmail = userEmail,
            Name = "Finance",
            Priority = priority++,
            MatchMode = RuleMatchMode.Any,
            IsGlobal = true,
            StopProcessing = true,
            Conditions = new List<MailRuleConditionEntity>
            {
                new() { Field = RuleConditionField.From, Operator = RuleConditionOperator.Contains, Value = "bank" }, // Generic
                new() { Field = RuleConditionField.Subject, Operator = RuleConditionOperator.Contains, Value = "Statement" }
            },
            Actions = new List<MailRuleActionEntity>
            {
                new() { ActionType = RuleActionType.MoveToFolder, TargetValue = "Finance" },
                new() { ActionType = RuleActionType.Flag }
            }
        });

        // 3. Transactional / Shopping
        rules.Add(new MailRuleEntity
        {
            UserEmail = userEmail,
            Name = "Shopping",
            Priority = priority++,
            MatchMode = RuleMatchMode.Any,
            IsGlobal = true,
            StopProcessing = true,
            Conditions = new List<MailRuleConditionEntity>
            {
                new() { Field = RuleConditionField.Subject, Operator = RuleConditionOperator.Contains, Value = "Order confirmation" },
                new() { Field = RuleConditionField.Subject, Operator = RuleConditionOperator.Contains, Value = "Receipt" },
                new() { Field = RuleConditionField.Subject, Operator = RuleConditionOperator.Contains, Value = "Invoice" },
                new() { Field = RuleConditionField.Subject, Operator = RuleConditionOperator.Contains, Value = "Dispatch" }
            },
            Actions = new List<MailRuleActionEntity>
            {
                new() { ActionType = RuleActionType.MoveToFolder, TargetValue = "Shopping" }
            }
        });

        // 4. Social
        rules.Add(new MailRuleEntity
        {
            UserEmail = userEmail,
            Name = "Social",
            Priority = priority++,
            MatchMode = RuleMatchMode.Any,
            IsGlobal = true,
            StopProcessing = true,
            Conditions = new List<MailRuleConditionEntity>
            {
                new() { Field = RuleConditionField.From, Operator = RuleConditionOperator.Contains, Value = "facebookmail.com" },
                new() { Field = RuleConditionField.From, Operator = RuleConditionOperator.Contains, Value = "linkedin.com" },
                new() { Field = RuleConditionField.From, Operator = RuleConditionOperator.Contains, Value = "twitter.com" },
                new() { Field = RuleConditionField.From, Operator = RuleConditionOperator.Contains, Value = "instagram.com" }
            },
            Actions = new List<MailRuleActionEntity>
            {
                new() { ActionType = RuleActionType.MoveToFolder, TargetValue = "Social" }
            }
        });

        // 5. Marketing
        rules.Add(new MailRuleEntity
        {
            UserEmail = userEmail,
            Name = "Marketing",
            Priority = priority++,
            MatchMode = RuleMatchMode.Any,
            IsGlobal = true,
            StopProcessing = true,
            Conditions = new List<MailRuleConditionEntity>
            {
                new() { Field = RuleConditionField.From, Operator = RuleConditionOperator.Contains, Value = "offers@" },
                new() { Field = RuleConditionField.From, Operator = RuleConditionOperator.Contains, Value = "promo@" },
                new() { Field = RuleConditionField.Subject, Operator = RuleConditionOperator.Contains, Value = "Sale" },
                new() { Field = RuleConditionField.Subject, Operator = RuleConditionOperator.Contains, Value = "Discount" }
            },
            Actions = new List<MailRuleActionEntity>
            {
                new() { ActionType = RuleActionType.MoveToFolder, TargetValue = "Marketing" }
            }
        });

        // 6. Mailing Lists
        rules.Add(new MailRuleEntity
        {
            UserEmail = userEmail,
            Name = "Mailing Lists",
            Priority = priority++,
            MatchMode = RuleMatchMode.Any,
            IsGlobal = true,
            StopProcessing = true,
            Conditions = new List<MailRuleConditionEntity>
            {
                new() { Field = RuleConditionField.ListId, Operator = RuleConditionOperator.MatchesRegex, Value = ".+" } // List-Id exists
            },
            Actions = new List<MailRuleActionEntity>
            {
                new() { ActionType = RuleActionType.MoveToFolder, TargetValue = "Mailing Lists" }
            }
        });

        // 7. Notifications (GitHub, Azure, Jira) - Catch-all for automated
        rules.Add(new MailRuleEntity
        {
            UserEmail = userEmail,
            Name = "Notifications",
            Priority = priority++,
            MatchMode = RuleMatchMode.Any,
            IsGlobal = true,
            StopProcessing = true,
            Conditions = new List<MailRuleConditionEntity>
            {
                new() { Field = RuleConditionField.From, Operator = RuleConditionOperator.Contains, Value = "github.com" },
                new() { Field = RuleConditionField.From, Operator = RuleConditionOperator.Contains, Value = "azure.com" },
                new() { Field = RuleConditionField.From, Operator = RuleConditionOperator.Contains, Value = "atlassian.net" },
                new() { Field = RuleConditionField.From, Operator = RuleConditionOperator.Contains, Value = "noreply" },
                new() { Field = RuleConditionField.IsAutoGenerated, Operator = RuleConditionOperator.Is, Value = "true" }
            },
            Actions = new List<MailRuleActionEntity>
            {
                new() { ActionType = RuleActionType.MoveToFolder, TargetValue = "Notifications" }
            }
        });

        return rules;
    }
}
