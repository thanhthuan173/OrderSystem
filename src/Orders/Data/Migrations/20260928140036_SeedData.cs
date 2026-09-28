using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Orders.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "orders",
                columns: new[] { "id", "created_at", "customer_id", "status", "total_amount", "updated_at" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 9, 28, 10, 0, 0, 0, DateTimeKind.Utc), "CUSTOMER-001", "Pending", 39.98m, new DateTime(2026, 9, 28, 10, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 9, 28, 10, 1, 0, 0, DateTimeKind.Utc), "CUSTOMER-002", "Reserving", 59.97m, new DateTime(2026, 9, 28, 10, 1, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 9, 28, 10, 2, 0, 0, DateTimeKind.Utc), "CUSTOMER-003", "Charging", 69.97m, new DateTime(2026, 9, 28, 10, 2, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 9, 28, 10, 3, 0, 0, DateTimeKind.Utc), "CUSTOMER-004", "Confirmed", 89.98m, new DateTime(2026, 9, 28, 10, 3, 0, 0, DateTimeKind.Utc) },
                    { new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 9, 28, 10, 4, 0, 0, DateTimeKind.Utc), "CUSTOMER-005", "Cancelled", 49.99m, new DateTime(2026, 9, 28, 10, 4, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "order_lines",
                columns: new[] { "id", "order_id", "quantity", "sku", "unit_price" },
                values: new object[,]
                {
                    { 1L, new Guid("11111111-1111-1111-1111-111111111111"), 2, "WIDGET-01", 19.99m },
                    { 2L, new Guid("22222222-2222-2222-2222-222222222222"), 2, "WIDGET-02", 29.99m },
                    { 3L, new Guid("33333333-3333-3333-3333-333333333333"), 2, "WIDGET-03", 34.99m },
                    { 4L, new Guid("44444444-4444-4444-4444-444444444444"), 2, "WIDGET-04", 44.99m },
                    { 5L, new Guid("55555555-5555-5555-5555-555555555555"), 1, "WIDGET-01", 49.99m }
                });

            migrationBuilder.InsertData(
                table: "order_saga_state",
                columns: new[] { "order_id", "last_processed_event_id" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), null }
                });

            migrationBuilder.InsertData(
                table: "order_saga_state",
                columns: new[] { "order_id", "last_processed_event_id", "reservation_completed" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), null, true });

            migrationBuilder.InsertData(
                table: "order_saga_state",
                columns: new[] { "order_id", "last_processed_event_id", "payment_completed", "reservation_completed" },
                values: new object[] { new Guid("44444444-4444-4444-4444-444444444444"), null, true, true });

            migrationBuilder.InsertData(
                table: "order_saga_state",
                columns: new[] { "order_id", "last_processed_event_id", "reservation_completed" },
                values: new object[] { new Guid("55555555-5555-5555-5555-555555555555"), null, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "order_lines",
                keyColumn: "id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "order_lines",
                keyColumn: "id",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "order_lines",
                keyColumn: "id",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "order_lines",
                keyColumn: "id",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                table: "order_lines",
                keyColumn: "id",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                table: "order_saga_state",
                keyColumn: "order_id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "order_saga_state",
                keyColumn: "order_id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "order_saga_state",
                keyColumn: "order_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "order_saga_state",
                keyColumn: "order_id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "order_saga_state",
                keyColumn: "order_id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "orders",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "orders",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "orders",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "orders",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "orders",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));
        }
    }
}
