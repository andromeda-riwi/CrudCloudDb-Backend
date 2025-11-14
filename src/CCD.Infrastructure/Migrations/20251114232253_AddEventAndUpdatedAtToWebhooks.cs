using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCD.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventAndUpdatedAtToWebhooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Event",
                table: "Webhooks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Webhooks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CredentialsViewed",
                table: "DatabaseInstances",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Event",
                table: "Webhooks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Webhooks");

            migrationBuilder.DropColumn(
                name: "CredentialsViewed",
                table: "DatabaseInstances");
        }
    }
}
