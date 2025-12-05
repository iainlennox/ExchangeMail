using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExchangeMail.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIsImportedToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsImported",
                table: "Messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Owner",
                table: "Messages",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsImported",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "Messages");
        }
    }
}
