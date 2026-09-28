using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Inventory.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "reservations",
                columns: new[] { "id", "created_at", "order_id", "quantity", "sku", "status" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 9, 28, 10, 2, 30, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), 2, "WIDGET-03", "Active" },
                    { 2L, new DateTime(2026, 9, 28, 10, 3, 30, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), 2, "WIDGET-04", "Consumed" },
                    { 3L, new DateTime(2026, 9, 28, 10, 4, 30, 0, DateTimeKind.Utc), new Guid("55555555-5555-5555-5555-555555555555"), 1, "WIDGET-01", "Released" }
                });

            migrationBuilder.InsertData(
                table: "stock_items",
                columns: new[] { "sku", "quantity_on_hand" },
                values: new object[] { "WIDGET-01", 100 });

            migrationBuilder.InsertData(
                table: "stock_items",
                columns: new[] { "sku", "quantity_on_hand", "quantity_reserved" },
                values: new object[,]
                {
                    { "WIDGET-02", 100, 2 },
                    { "WIDGET-03", 100, 2 }
                });

            migrationBuilder.InsertData(
                table: "stock_items",
                columns: new[] { "sku", "quantity_on_hand" },
                values: new object[] { "WIDGET-04", 98 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "reservations",
                keyColumn: "id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "reservations",
                keyColumn: "id",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "reservations",
                keyColumn: "id",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "stock_items",
                keyColumn: "sku",
                keyValue: "WIDGET-01");

            migrationBuilder.DeleteData(
                table: "stock_items",
                keyColumn: "sku",
                keyValue: "WIDGET-02");

            migrationBuilder.DeleteData(
                table: "stock_items",
                keyColumn: "sku",
                keyValue: "WIDGET-03");

            migrationBuilder.DeleteData(
                table: "stock_items",
                keyColumn: "sku",
                keyValue: "WIDGET-04");
        }
    }
}
