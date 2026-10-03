using Microsoft.EntityFrameworkCore;
using Payments.Data.Entities;

namespace Payments.Data
{
    public sealed class PaymentsDbContext (DbContextOptions<PaymentsDbContext> options)
        :DbContext(options)
    {
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigurePayment(modelBuilder);
            ConfigureOutboxMessage(modelBuilder);
            ConfigureInboxMessage(modelBuilder);

            SeedPayments(modelBuilder);
        }

        private static void ConfigurePayment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("payments");

                entity.HasKey(payment => payment.Id);

                entity.Property(payment => payment.Id)
                    .HasColumnName("id");

                entity.Property(payment => payment.OrderId)
                    .HasColumnName("order_id")
                    .IsRequired();

                entity.Property(payment => payment.Amount)
                    .HasColumnName("amount")
                    .HasPrecision(10, 2)
                    .IsRequired();

                entity.Property(payment => payment.Status)
                    .HasColumnName("status")
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(payment => payment.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("NOW()")
                    .IsRequired();

                entity.ToTable(payment => payment.HasCheckConstraint(
                    "CK_payments_status",
                    "\"status\" IN ('Succeeded', 'Failed')"));

                entity.HasIndex(payment => payment.OrderId)
                    .IsUnique();
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

        private static void SeedPayments(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>().HasData(
                new Payment
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    OrderId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Amount = 89.98m,
                    Status = PaymentStatus.Succeeded,
                    CreatedAt = new DateTime(
                        2026, 9, 28, 10, 3, 45,
                        DateTimeKind.Utc)
                },
                new Payment
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    OrderId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Amount = 49.99m,
                    Status = PaymentStatus.Failed,
                    CreatedAt = new DateTime(
                        2026, 9, 28, 10, 4, 45,
                        DateTimeKind.Utc)
                }
            );
        }
    }
}
