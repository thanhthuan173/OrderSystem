using Contracts.Events;
using DotPulsar.Abstractions;
using Messaging;
using Orders.Data;
using System.Text.Json;

namespace Orders.Messaging
{
    public class OrdersReservationSucceededConsumer : PulsarConsumerBackgroundService
    {
        private readonly OrdersDbContext _db;

        protected override string Topic =>
            PulsarTopics.ReservationSucceeded;

        protected override string SubscriptionName =>
            "orders-reservation-succeeded";

        public OrdersReservationSucceededConsumer(
            IPulsarClient client,
            OrdersDbContext db,
            ILogger<OrdersReservationSucceededConsumer> logger)
            : base(client, logger)
        {
            _db = db;
        }

        protected override async Task HandleMessageAsync(
            string payload,
            CancellationToken cancellationToken)
        {
            var @event =
                JsonSerializer.Deserialize<ReservationSucceededEvent>(
                    payload);

            if (@event == null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize ReservationSucceededEvent.");
            }

            Console.WriteLine(
                $"Orders received ReservationSucceeded for {@event.OrderId}");

            // Business logic will be implemented later.
            await Task.CompletedTask;
        }
    }
}
