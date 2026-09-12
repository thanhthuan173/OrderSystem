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
        }

        private static void ConfigurePayment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("payments");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("id");

                entity.Property(x => x.OrderId)
                    .HasColumnName("order_id")
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasColumnName("amount")
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

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_payments_status",
                    "\"status\" IN ('Succeeded', 'Failed')"));

                entity.HasIndex(x => x.OrderId)
                    .IsUnique();
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
