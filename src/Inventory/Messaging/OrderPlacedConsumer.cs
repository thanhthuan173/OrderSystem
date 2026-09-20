using Contracts.Events;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Inventory.Services;
using System.Text.Json;

namespace Inventory.Messaging
{
    public sealed class OrderPlacedConsumer : BackgroundService
    {
        private const string OrderPlacedTopic =
            "persistent://public/default/order-placed";

        private const string InventoryOrderPlacedSubscription =
            "inventory-order-placed";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPulsarClient _pulsarClient;
        private readonly ILogger<OrderPlacedConsumer> _logger;

        public OrderPlacedConsumer(
            IServiceScopeFactory scopeFactory,
            IPulsarClient pulsarClient,
            ILogger<OrderPlacedConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _pulsarClient = pulsarClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken cancellationToken)
        {
            await using var consumer = _pulsarClient
                    .NewConsumer(Schema.String)
                    .Topic(OrderPlacedTopic)
                    .SubscriptionName(InventoryOrderPlacedSubscription)
                    .InitialPosition(SubscriptionInitialPosition.Earliest)
                    .Create();

            await foreach (var message in consumer.Messages(cancellationToken))
            {
                try
                {
                    var @event =
                        JsonSerializer.Deserialize<OrderPlacedEvent>(
                            message.Value())
                        ?? throw new InvalidOperationException(
                            "Could not deserialize OrderPlaced event.");

                    using var scope = _scopeFactory.CreateScope();

                    var inventoryService =
                        scope.ServiceProvider
                            .GetRequiredService<InventoryService>();

                    await inventoryService.HandleOrderPlacedAsync(
                        @event,
                        cancellationToken);

                    await consumer.Acknowledge(
                        message,
                        cancellationToken);

                    _logger.LogInformation(
                        "Processed OrderPlaced event {EventId} for order {OrderId}",
                        @event.EventId,
                        @event.OrderId);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to process OrderPlaced message.");

                    // Tạm thời chưa xử lý retry/DLQ.
                    // Đừng để retry làm nhiễu quá trình debug connection.
                }
            }
        }
    }
}
