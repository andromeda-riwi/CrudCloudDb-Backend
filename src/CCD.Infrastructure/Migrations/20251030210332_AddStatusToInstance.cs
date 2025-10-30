using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCD.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToInstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DbUsername",
                table: "DatabaseInstances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Host",
                table: "DatabaseInstances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Port",
                table: "DatabaseInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DbUsername",
                table: "DatabaseInstances");

            migrationBuilder.DropColumn(
                name: "Host",
                table: "DatabaseInstances");

            migrationBuilder.DropColumn(
                name: "Port",
                table: "DatabaseInstances");
        }
    }
}
