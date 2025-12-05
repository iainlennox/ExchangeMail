using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExchangeMail.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEnableAnimationsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableAnimations",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableAnimations",
                table: "Users");
        }
    }
}
