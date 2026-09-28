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

            SeedData(modelBuilder);
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

        private static void SeedData(ModelBuilder modelBuilder)
        {
            SeedOrders(modelBuilder);
            SeedOrderLines(modelBuilder);
            SeedOrderSagaStates(modelBuilder);
        }

        private static void SeedOrders(ModelBuilder modelBuilder)
        {
            var baseTime = new DateTime(
                2026, 9, 28, 10, 0, 0,
                DateTimeKind.Utc);

            modelBuilder.Entity<Order>().HasData(
                new Order
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    CustomerId = "CUSTOMER-001",
                    TotalAmount = 39.98m,
                    Status = OrderStatus.Pending,
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime
                },
                new Order
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    CustomerId = "CUSTOMER-002",
                    TotalAmount = 59.97m,
                    Status = OrderStatus.Reserving,
                    CreatedAt = baseTime.AddMinutes(1),
                    UpdatedAt = baseTime.AddMinutes(1)
                },
                new Order
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    CustomerId = "CUSTOMER-003",
                    TotalAmount = 69.97m,
                    Status = OrderStatus.Charging,
                    CreatedAt = baseTime.AddMinutes(2),
                    UpdatedAt = baseTime.AddMinutes(2)
                },
                new Order
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    CustomerId = "CUSTOMER-004",
                    TotalAmount = 89.98m,
                    Status = OrderStatus.Confirmed,
                    CreatedAt = baseTime.AddMinutes(3),
                    UpdatedAt = baseTime.AddMinutes(3)
                },
                new Order
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    CustomerId = "CUSTOMER-005",
                    TotalAmount = 49.99m,
                    Status = OrderStatus.Cancelled,
                    CreatedAt = baseTime.AddMinutes(4),
                    UpdatedAt = baseTime.AddMinutes(4)
                }
            );
        }

        private static void SeedOrderLines(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderLine>().HasData(
                new OrderLine
                {
                    Id = 1,
                    OrderId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Sku = "WIDGET-01",
                    Quantity = 2,
                    UnitPrice = 19.99m
                },
                new OrderLine
                {
                    Id = 2,
                    OrderId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Sku = "WIDGET-02",
                    Quantity = 2,
                    UnitPrice = 29.99m
                },
                new OrderLine
                {
                    Id = 3,
                    OrderId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Sku = "WIDGET-03",
                    Quantity = 2,
                    UnitPrice = 34.99m
                },
                new OrderLine
                {
                    Id = 4,
                    OrderId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Sku = "WIDGET-04",
                    Quantity = 2,
                    UnitPrice = 44.99m
                },
                new OrderLine
                {
                    Id = 5,
                    OrderId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Sku = "WIDGET-01",
                    Quantity = 1,
                    UnitPrice = 49.99m
                }
            );
        }

        private static void SeedOrderSagaStates(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderSagaState>().HasData(
                new OrderSagaState
                {
                    OrderId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    ReservationCompleted = false,
                    PaymentCompleted = false,
                    LastProcessedEventId = null
                },
                new OrderSagaState
                {
                    OrderId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    ReservationCompleted = false,
                    PaymentCompleted = false,
                    LastProcessedEventId = null
                },
                new OrderSagaState
                {
                    OrderId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    ReservationCompleted = true,
                    PaymentCompleted = false,
                    LastProcessedEventId = null
                },
                new OrderSagaState
                {
                    OrderId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    ReservationCompleted = true,
                    PaymentCompleted = true,
                    LastProcessedEventId = null
                },
                new OrderSagaState
                {
                    OrderId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    ReservationCompleted = true,
                    PaymentCompleted = false,
                    LastProcessedEventId = null
                }
            );
        }
    }
}
