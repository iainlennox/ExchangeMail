using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExchangeMail.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMailRuleEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MoveToFolder",
                table: "MailRules");

            migrationBuilder.DropColumn(
                name: "SenderContains",
                table: "MailRules");

            migrationBuilder.DropColumn(
                name: "SubjectContains",
                table: "MailRules");

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "MailRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsGlobal",
                table: "MailRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MatchMode",
                table: "MailRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "MailRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "StopProcessing",
                table: "MailRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MailRuleActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionType = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetValue = table.Column<string>(type: "TEXT", nullable: true),
                    MailRuleEntityId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailRuleActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailRuleActions_MailRules_MailRuleEntityId",
                        column: x => x.MailRuleEntityId,
                        principalTable: "MailRules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MailRuleConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Field = table.Column<int>(type: "INTEGER", nullable: false),
                    Operator = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    MailRuleEntityId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailRuleConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailRuleConditions_MailRules_MailRuleEntityId",
                        column: x => x.MailRuleEntityId,
                        principalTable: "MailRules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailRuleActions_MailRuleEntityId",
                table: "MailRuleActions",
                column: "MailRuleEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_MailRuleConditions_MailRuleEntityId",
                table: "MailRuleConditions",
                column: "MailRuleEntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailRuleActions");

            migrationBuilder.DropTable(
                name: "MailRuleConditions");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "MailRules");

            migrationBuilder.DropColumn(
                name: "IsGlobal",
                table: "MailRules");

            migrationBuilder.DropColumn(
                name: "MatchMode",
                table: "MailRules");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "MailRules");

            migrationBuilder.DropColumn(
                name: "StopProcessing",
                table: "MailRules");

            migrationBuilder.AddColumn<string>(
                name: "MoveToFolder",
                table: "MailRules",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SenderContains",
                table: "MailRules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectContains",
                table: "MailRules",
                type: "TEXT",
                nullable: true);
        }
    }
}
