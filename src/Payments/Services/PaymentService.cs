using System.Text.Json;
using Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Payments.Data;
using Payments.Data.Entities;
using Payments.Models;

namespace Payments.Services
{
    public class PaymentService
    {
        private const string FailureReason = "The order total ends in .99";
        private const string PaymentFailed_Topic = "persistent://public/default/payment-failed";
        private const string PaymentSucceeded_Topic = "persistent://public/default/payment-succeeded";

        private readonly PaymentsDbContext _db;

        public PaymentService(PaymentsDbContext db)
        {
            _db = db;
        }

        public async Task<GetPaymentResponse> GetPaymentAsync(Guid orderId, CancellationToken cancellationToken)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken)
                ??throw new Exception("Payment not found");

            return new GetPaymentResponse(
                payment.Id,
                payment.OrderId,
                payment.Amount,
                payment.Status.ToString(),
                payment.CreatedAt);
        }

        public async Task HandleReservationSucceededAsync(
            ReservationSucceededEvent @event,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var alreadyProcessed=await _db.InboxMessages
                .AnyAsync(i=>i.EventId==@event.EventId, cancellationToken);

            if (alreadyProcessed)
            {
                return;
            }

            decimal total = 0;

            foreach(var line in @event.Lines)
            {
                total += line.Quantity * line.UnitPrice;
            }

            EventBase paymentResult;
            string topic;

            if (total % 1 == (decimal)0.99)
            {
                topic = PaymentFailed_Topic;

                paymentResult = new PaymentFailedEvent(
                    Guid.NewGuid(),
                    @event.OrderId,
                    FailureReason);
            }
            else
            {
                topic = PaymentSucceeded_Topic;

                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = @event.OrderId,
                    Amount = total,
                    Status = PaymentStatus.Succeeded,
                    CreatedAt = DateTime.UtcNow
                };

                paymentResult = new PaymentSucceededEvent(
                    Guid.NewGuid(),
                    @event.OrderId,
                    payment.Id,
                    total);
            }

            _db.InboxMessages.Add(new InboxMessage
            {
                EventId = @event.EventId,
                ProcessedAt = DateTime.UtcNow
            });

            _db.OutboxMessages.Add(new OutboxMessage
            {
                EventId = paymentResult.EventId,
                Topic = topic,
                Payload = JsonSerializer.Serialize(paymentResult),
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
