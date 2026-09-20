using System.Text.Json;
using Contracts.Events;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Orders.Services;

namespace Orders.Messaging
{
    public sealed class ReservationFailedConsumer : BackgroundService
    {
        private const string ReservationFailed_Topic = 
            "persistent://public/default/reservation-failed";
        private const string Order_ReservationFailed_Subscription = 
            "order-reservation-failed";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPulsarClient _pulsarClient;
        private readonly ILogger<ReservationFailedConsumer> _logger;

        public ReservationFailedConsumer(
            IServiceScopeFactory scopeFactory,
            IPulsarClient pulsarClient,
            ILogger<ReservationFailedConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _pulsarClient = pulsarClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await using var consumer = _pulsarClient
                .NewConsumer(Schema.String)
                .Topic(ReservationFailed_Topic)
                .SubscriptionName(Order_ReservationFailed_Subscription)
                .InitialPosition(SubscriptionInitialPosition.Earliest)
                .Create();

            await foreach(var message in consumer.Messages(cancellationToken))
            {
                try
                {
                    var @event = JsonSerializer.Deserialize<ReservationFailedEvent>(message.Value())
                    ?? throw new InvalidOperationException("Could not deserialize ReservationSucceeded event.");

                    using var scope = _scopeFactory.CreateScope();
                    var orderService = scope.ServiceProvider
                        .GetRequiredService<OrderService>();

                    await orderService.HandleReservationAsync(
                        @event,
                        cancellationToken,
                        true);

                    await consumer.Acknowledge(message, cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {

                }
                catch (Exception ex)
                {
                    _logger.LogError(
                   ex,
                   "Failed to process OrderPlaced message.");

                    // Temporary retry mechanism for this first slice.
                    // DLQ/retry policy will be added later.
                    await consumer.RedeliverUnacknowledgedMessages(
                        new[] { message.MessageId },
                        cancellationToken);
                }
            }
        }
    }
}
