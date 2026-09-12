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
        }

        private static void ConfigureStockItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockItem>(entity =>
            {
                entity.ToTable("stock_items");

                entity.HasKey(x => x.Sku);

                entity.Property(x => x.Sku)
                    .HasColumnName("sku")
                    .HasMaxLength(50);

                entity.Property(x => x.QuantityOnHand)
                    .HasColumnName("quantity_on_hand")
                    .IsRequired();

                entity.Property(x => x.QuantityReserved)
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

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("id");

                entity.Property(x => x.OrderId)
                    .HasColumnName("order_id")
                    .IsRequired();

                entity.Property(x => x.Sku)
                    .HasColumnName("sku")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Quantity)
                    .HasColumnName("quantity")
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasColumnName("status")
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("NOW()")
                    .IsRequired();

                entity.HasIndex(x => new { x.OrderId, x.Sku })
                    .IsUnique();

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_reservations_status",
                    "\"status\" IN ('Active', 'Released', 'Consumed')"));
            });
        }

        private static void ConfigureOutboxMessage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("outbox_messages");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("id");

                entity.Property(x => x.EventId)
                    .HasColumnName("event_id")
                    .IsRequired();

                entity.HasIndex(x => x.EventId)
                    .IsUnique();

                entity.Property(x => x.Topic)
                    .HasColumnName("topic")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.Payload)
                    .HasColumnName("payload")
                    .HasColumnType("jsonb")
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("NOW()")
                    .IsRequired();

                entity.Property(x => x.PublishedAt)
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

                entity.HasKey(x => x.EventId);

                entity.Property(x => x.EventId)
                    .HasColumnName("event_id");

                entity.Property(x => x.ProcessedAt)
                    .HasColumnName("processed_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("NOW()")
                    .IsRequired();
            });
        }
    }
}