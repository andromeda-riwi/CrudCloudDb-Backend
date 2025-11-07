using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCD.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeZoneToDatabaseInstances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "DatabaseInstances",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "DatabaseInstances");
        }
    }
}
