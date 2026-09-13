using Contracts.Events;
using DotPulsar.Abstractions;
using Messaging;
using System.Text.Json;

namespace Payments.Messaging
{
    public class PaymentsReservationSucceededConsumer
    : PulsarConsumerBackgroundService
    {
        protected override string Topic =>
            PulsarTopics.ReservationSucceeded;

        protected override string SubscriptionName =>
            "payments-reservation-succeeded";

        public PaymentsReservationSucceededConsumer(
            IPulsarClient client,
            ILogger<PaymentsReservationSucceededConsumer> logger)
            : base(client, logger)
        {
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
                $"Payments received ReservationSucceeded: {@event.OrderId}");

            await Task.CompletedTask;
        }
    }
}
