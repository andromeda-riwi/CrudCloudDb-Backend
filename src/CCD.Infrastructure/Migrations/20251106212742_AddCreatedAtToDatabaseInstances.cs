using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCD.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtToDatabaseInstances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DatabaseInstances",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.Sql("UPDATE \"DatabaseInstances\" SET \"CreatedAt\" = NOW() WHERE \"CreatedAt\" = TIMESTAMPTZ '-infinity' OR \"CreatedAt\" = TIMESTAMPTZ '0001-01-01 00:00:00Z'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DatabaseInstances");
        }
    }
}
