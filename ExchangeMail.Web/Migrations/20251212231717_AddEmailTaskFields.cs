using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExchangeMail.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailTaskFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailDate",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailMessageId",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailSender",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailSubject",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Tasks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailDate",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "EmailMessageId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "EmailSender",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "EmailSubject",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tasks");
        }
    }
}
