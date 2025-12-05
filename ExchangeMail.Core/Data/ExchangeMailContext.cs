using ExchangeMail.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMail.Core.Data;

public class ExchangeMailContext : DbContext
{
    public ExchangeMailContext(DbContextOptions<ExchangeMailContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=exchangemail.db");
        }
    }

    public DbSet<UserEntity> Users { get; set; }
    public DbSet<MessageEntity> Messages { get; set; }
    public DbSet<UserMessageEntity> UserMessages { get; set; }
    public DbSet<FolderEntity> Folders { get; set; }
    public DbSet<ConfigEntity> Configurations { get; set; }
    public DbSet<ContactEntity> Contacts { get; set; }
    public DbSet<LogEntity> Logs { get; set; }
    public DbSet<SafeSenderEntity> SafeSenders { get; set; }
    public DbSet<BlockedSenderEntity> BlockedSenders { get; set; }
    public DbSet<MailRuleEntity> MailRules { get; set; }
    public DbSet<MailRuleConditionEntity> MailRuleConditions { get; set; }
    public DbSet<MailRuleActionEntity> MailRuleActions { get; set; }
}
