using Microsoft.EntityFrameworkCore;
using Orders.Data.Entities;

namespace Orders.Data
{
    public sealed class OrdersDbContext (DbContextOptions<OrdersDbContext> options)
        : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderLine> OrderLines => Set<OrderLine>();
        public DbSet<OrderSagaState> OrderSagaStates => Set<OrderSagaState>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureOrder(modelBuilder);
            ConfigureOrderLine(modelBuilder);
            ConfigureOrderSagaState(modelBuilder);
            ConfigureOutboxMessage(modelBuilder);
            ConfigureInboxMessage(modelBuilder);
        }

        private static void ConfigureOrder(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("orders");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("id");

                entity.Property(x => x.CustomerId)
                    .HasColumnName("customer_id")
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.TotalAmount)
                    .HasColumnName("total_amount")
                    .HasPrecision(10, 2)
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

                entity.Property(x => x.UpdatedAt)
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("NOW()")
                    .IsRequired();

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_orders_status",
                    "\"status\" IN ('Pending', 'Reserving', 'Charging', 'Confirmed', 'Cancelled')"
                ));
            });
        }

        private static void ConfigureOrderLine(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderLine>(entity =>
            {
                entity.ToTable("order_lines");

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

                entity.Property(x => x.UnitPrice)
                    .HasColumnName("unit_price")
                    .HasPrecision(10, 2)
                    .IsRequired();

                entity.HasOne(x => x.Order)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureOrderSagaState(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderSagaState>(entity =>
            {
                entity.ToTable("order_saga_state");

                entity.HasKey(x => x.OrderId);

                entity.Property(x => x.OrderId)
                    .HasColumnName("order_id");

                entity.Property(x => x.ReservationCompleted)
                    .HasColumnName("reservation_completed")
                    .HasDefaultValue(false)
                    .IsRequired();

                entity.Property(x => x.PaymentCompleted)
                    .HasColumnName("payment_completed")
                    .HasDefaultValue(false)
                    .IsRequired();

                entity.Property(x => x.LastProcessedEventId)
                    .HasColumnName("last_processed_event_id")
                    .IsRequired(false);

                entity.HasOne(x => x.Order)
                    .WithOne(x => x.SagaState)
                    .HasForeignKey<OrderSagaState>(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
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
