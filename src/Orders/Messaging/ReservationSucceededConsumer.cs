using System.Text.Json;
using Contracts.Events;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Orders.Services;

namespace Orders.Messaging
{
    public sealed class ReservationSucceededConsumer : BackgroundService
    {
        private const string ReservationSucceeded_Topic = 
            "persistent://public/default/reservation-succeeded";
        private const string Orders_ReservationSucceeded_Subscription = 
            "orders-reservation-succeeded";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPulsarClient _pulsarClient;
        private readonly ILogger<ReservationSucceededConsumer> _logger;

        public ReservationSucceededConsumer(
            IServiceScopeFactory scopeFactory,
            IPulsarClient pulsarClient,
            ILogger<ReservationSucceededConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _pulsarClient = pulsarClient;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await using var consumer = _pulsarClient
                .NewConsumer(Schema.String)
                .Topic(ReservationSucceeded_Topic)
                .SubscriptionName(Orders_ReservationSucceeded_Subscription)
                .InitialPosition(SubscriptionInitialPosition.Earliest)
                .Create();

            await foreach(var message in consumer.Messages(cancellationToken))
            {
                try
                {
                    var @event = JsonSerializer.Deserialize<ReservationSucceededEvent>(message.Value())
                        ?? throw new InvalidOperationException("Could not deserialize ReservationSucceeded event.");

                    using var scope = _scopeFactory.CreateScope();
                    var orderService = scope.ServiceProvider
                        .GetRequiredService<OrderService>();

                    await orderService.HandleReservationSucceededAsync(@event, cancellationToken);

                    await consumer.Acknowledge(message, cancellationToken);
                }
                catch (OperationCanceledException)
                    when(cancellationToken.IsCancellationRequested)
                {
                    break;
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
