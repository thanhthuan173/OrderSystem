using System.Text.Json;
using Contracts.Events;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Inventory.Services;

namespace Inventory.Messaging
{
    public sealed class PaymentFailedConsumer : BackgroundService
    {
        private const string PaymentFailed_Topic = 
            "persistent://public/default/payment-failed";
        private const string Inventory_PaymentFailed_Subscription = 
            "inventory-payment-failed";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPulsarClient _pulsarClient;
        private readonly ILogger _logger;

        public PaymentFailedConsumer(
            IServiceScopeFactory scopeFactory,
            IPulsarClient pulsarClient,
            ILogger logger)
        {
            _scopeFactory = scopeFactory;
            _pulsarClient = pulsarClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await using var consumer = _pulsarClient
                .NewConsumer(Schema.String)
                .Topic(PaymentFailed_Topic)
                .SubscriptionName(Inventory_PaymentFailed_Subscription)
                .InitialPosition(SubscriptionInitialPosition.Earliest)
                .Create();

            await foreach(var message in consumer.Messages())
            {
                try
                {
                    var @event = JsonSerializer.Deserialize<PaymentFailedEvent>(message.Value())
                        ?? throw new InvalidOperationException("Could not deserialize OrderPlaced event.");

                    using var scope = _scopeFactory.CreateScope();
                    var inventoryService = scope.ServiceProvider
                        .GetRequiredService<InventoryService>();

                    await inventoryService.HandlePaymentFailedAsync(@event, cancellationToken);

                    await consumer.Acknowledge(message);
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
