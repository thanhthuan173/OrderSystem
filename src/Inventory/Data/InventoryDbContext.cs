using Inventory.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Data
{
    public sealed class InventoryDbContext (DbContextOptions<InventoryDbContext> option)
        : DbContext(option)
    {
        public DbSet<StockItem> StockItems => Set<StockItem>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureStockItem(modelBuilder);
            ConfigureReservation(modelBuilder);
            ConfigureOutboxMessage(modelBuilder);
            ConfigureInboxMessage(modelBuilder);

            SeedStockItems(modelBuilder);
            SeedReservations(modelBuilder);
        }

        private static void ConfigureStockItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockItem>(entity =>
            {
                entity.ToTable("stock_items");

                entity.HasKey(item => item.Sku);

                entity.Property(item => item.Sku)
                    .HasColumnName("sku")
                    .HasMaxLength(50);

                entity.Property(item => item.QuantityOnHand)
                    .HasColumnName("quantity_on_hand")
                    .IsRequired();

                entity.Property(item => item.QuantityReserved)
                    .HasColumnName("quantity_reserved")
                    .HasDefaultValue(0)
                    .IsRequired();
            });
        }

        private static void ConfigureReservation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.ToTable("reservations");

                entity.HasKey(reservation => reservation.Id);

                entity.Property(reservation => reservation.Id)
                    .HasColumnName("id");

                entity.Property(reservation => reservation.OrderId)
                    .HasColumnName("order_id")
                    .IsRequired();

                entity.Property(reservation => reservation.Sku)
                    .HasColumnName("sku")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(reservation => reservation.Quantity)
                    .HasColumnName("quantity")
                    .IsRequired();

                entity.Property(reservation => reservation.Status)
                    .HasColumnName("status")
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(reservation => reservation.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("NOW()")
                    .IsRequired();

                entity.HasIndex(reservation => new { reservation.OrderId, reservation.Sku })
                    .IsUnique();

                entity.ToTable(reservation => reservation.HasCheckConstraint(
                    "CK_reservations_status",
                    "\"status\" IN ('Active', 'Released', 'Consumed')"));
            });
        }

        private static void ConfigureOutboxMessage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("outbox_messages");

                entity.HasKey(message => message.Id);

                entity.Property(message => message.Id)
                    .HasColumnName("id");

                entity.Property(message => message.EventId)
                    .HasColumnName("event_id")
                    .IsRequired();

                entity.HasIndex(message => message.EventId)
                    .IsUnique();

                entity.Property(message => message.Topic)
                    .HasColumnName("topic")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(message => message.Payload)
                    .HasColumnName("payload")
                    .HasColumnType("jsonb")
                    .IsRequired();

                entity.Property(message => message.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("NOW()")
                    .IsRequired();

                entity.Property(message => message.PublishedAt)
                    .HasColumnName("published_at")
                    .HasColumnType("timestamp with time zone")
                    .IsRequired(false);
            });
        }

        private static void ConfigureInboxMessage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InboxMessage>(entity =>
            {
                entity.ToTable("inbox_messages");

                entity.HasKey(message => message.EventId);

                entity.Property(message => message.EventId)
                    .HasColumnName("event_id");

                entity.Property(message => message.ProcessedAt)
                    .HasColumnName("processed_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("NOW()")
                    .IsRequired();
            });
        }

        private static void SeedStockItems(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockItem>().HasData(
                new StockItem
                {
                    Sku = "WIDGET-01",
                    QuantityOnHand = 100,
                    QuantityReserved = 0
                },
                new StockItem
                {
                    Sku = "WIDGET-02",
                    QuantityOnHand = 100,
                    QuantityReserved = 2
                },
                new StockItem
                {
                    Sku = "WIDGET-03",
                    QuantityOnHand = 100,
                    QuantityReserved = 2
                },
                new StockItem
                {
                    Sku = "WIDGET-04",
                    QuantityOnHand = 98,
                    QuantityReserved = 0
                }
            );
        }

        private static void SeedReservations(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reservation>().HasData(
                new Reservation
                {
                    Id = 1,
                    OrderId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Sku = "WIDGET-03",
                    Quantity = 2,
                    Status = ReservationStatus.Active,
                    CreatedAt = new DateTime(
                        2026, 9, 28, 10, 2, 30,
                        DateTimeKind.Utc)
                },
                new Reservation
                {
                    Id = 2,
                    OrderId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Sku = "WIDGET-04",
                    Quantity = 2,
                    Status = ReservationStatus.Consumed,
                    CreatedAt = new DateTime(
                        2026, 9, 28, 10, 3, 30,
                        DateTimeKind.Utc)
                },
                new Reservation
                {
                    Id = 3,
                    OrderId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Sku = "WIDGET-01",
                    Quantity = 1,
                    Status = ReservationStatus.Released,
                    CreatedAt = new DateTime(
                        2026, 9, 28, 10, 4, 30,
                        DateTimeKind.Utc)
                }
            );
        }
    }
}