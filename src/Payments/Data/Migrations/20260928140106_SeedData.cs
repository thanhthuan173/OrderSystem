using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Payments.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "payments",
                columns: new[] { "id", "amount", "created_at", "order_id", "status" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 89.98m, new DateTime(2026, 9, 28, 10, 3, 45, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), "Succeeded" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 49.99m, new DateTime(2026, 9, 28, 10, 4, 45, 0, DateTimeKind.Utc), new Guid("55555555-5555-5555-5555-555555555555"), "Failed" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "payments",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "payments",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        }
    }
}
