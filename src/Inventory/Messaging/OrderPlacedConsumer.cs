using System.Text.Json;
using Contracts.Events;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Inventory.Models;
using Inventory.Services;

namespace Inventory.Messaging
{
    public sealed class OrderPlacedConsumer : BackgroundService
    {
        private const string OrderPlaced_Topic =
            "persistent://public/default/order-placed";
        private const string Inventory_OrderPlaced_Subscription =
            "inventory-order-placed";
        private const string Inventory_DeadLetterTopic =
            "persistent://public/default/inventory-dlq";
        private const int MaxDeliveryAttempts = 3;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPulsarClient _pulsarClient;
        private readonly PulsarHealthState _state;
        private readonly ILogger<OrderPlacedConsumer> _logger;
        
        private Dictionary<string, int> _redeliveryCount = [];

        public OrderPlacedConsumer(
            IServiceScopeFactory scopeFactory,
            IPulsarClient pulsarClient,
            PulsarHealthState state,
            ILogger<OrderPlacedConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _pulsarClient = pulsarClient;
            _state = state;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken cancellationToken)
        {
            await using var consumer = _pulsarClient
                .NewConsumer(Schema.String)
                .Topic(OrderPlaced_Topic)
                .SubscriptionName(Inventory_OrderPlaced_Subscription)
                .InitialPosition(SubscriptionInitialPosition.Earliest)
                .StateChangedHandler(stateChanged =>
                {
                    _state.Update(stateChanged);

                    _logger.LogInformation(
                        "Pulsar consumer {Topic}/{Subscription} changed state to {State}",
                        stateChanged.Consumer.Topic,
                        stateChanged.Consumer.SubscriptionName,
                        stateChanged.ConsumerState);
                })
                .Create();

            await using var deadLetterProducer = _pulsarClient
                .NewProducer(Schema.String)
                .Topic(Inventory_DeadLetterTopic)
                .Create();

            await foreach (var message in consumer.Messages(cancellationToken))
            {
                try
                {
                    var @event =
                        JsonSerializer.Deserialize<OrderPlacedEvent>(message.Value())
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
                    var messageId = message.MessageId.ToString();

                    _redeliveryCount.TryGetValue(messageId, out var currentAttempt);

                    var deliveryAttempt = currentAttempt + 1;

                    _logger.LogError(
                        ex,
                        "Failed to process message {MessageId}. Deliver attempt: {DeliverAttempt}/{MaxDeliverAttempts}",
                        message.MessageId,
                        deliveryAttempt,
                        MaxDeliveryAttempts);

                    try
                    {
                        if (deliveryAttempt >= MaxDeliveryAttempts)
                        {
                            var deadLetterMessage = new DeadLetterMessage
                            {
                                OriginalTopic = OrderPlaced_Topic,
                                SubscriptionName = Inventory_OrderPlaced_Subscription,
                                MessageId = message.MessageId.ToString(),
                                Payload = message.Value(),
                                ErrorMessage = ex.Message,
                                FailedAt = DateTime.UtcNow
                            };

                            await deadLetterProducer.Send(
                                JsonSerializer.Serialize(deadLetterMessage), 
                                cancellationToken);

                            await consumer.Acknowledge(message, cancellationToken);

                            _redeliveryCount.Remove(messageId);

                            _logger.LogInformation(
                                "Message {MessageId} moved to DLQ",
                                message.MessageId);
                        }
                        else
                        {
                            _redeliveryCount[messageId] = deliveryAttempt;

                            await consumer.RedeliverUnacknowledgedMessages(
                                new[] { message.MessageId },
                                cancellationToken);
                        }
                    }
                    catch (Exception redeliveryException)
                    {
                        _logger.LogError(
                            redeliveryException,
                            "Failed to redeliver message {MessageId}",
                            message.MessageId);
                    }
                }
            }
        }
    }
}
