using System.Text.Json;
using Contracts.Events;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Orders.Models;
using Orders.Services;

namespace Orders.Messaging
{
    public sealed class ReservationFailedConsumer : BackgroundService
    {
        private const string ReservationFailed_Topic = 
            "persistent://public/default/reservation-failed";
        private const string Orders_ReservationFailed_Subscription = 
            "orders-reservation-failed";
        private const string Orders_DeadLetterTopic =
            "persistent://public/default/orders-dlq";
        private const int MaxDeliveryAttempts = 3;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPulsarClient _pulsarClient;
        private readonly PulsarHealthState _state;
        private readonly ILogger<ReservationFailedConsumer> _logger;

        private readonly Dictionary<string, int> _redeliveryCount = [];

        public ReservationFailedConsumer(
            IServiceScopeFactory scopeFactory,
            IPulsarClient pulsarClient,
            PulsarHealthState state,
            ILogger<ReservationFailedConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _pulsarClient = pulsarClient;
            _state = state;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await using var consumer = _pulsarClient
                .NewConsumer(Schema.String)
                .Topic(ReservationFailed_Topic)
                .SubscriptionName(Orders_ReservationFailed_Subscription)
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
                .Topic(Orders_DeadLetterTopic)
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

                    await orderService.HandleReservationFailedAsync(
                        @event,
                        cancellationToken);

                    await consumer.Acknowledge(message, cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {

                }
                catch (Exception ex)
                {
                    var messageId = message.MessageId.ToString();

                    _redeliveryCount.TryGetValue(messageId, out var currentAttempt);

                    var deliveryAttempt = currentAttempt + 1;

                    _logger.LogError(
                        ex,
                        "Failed to process message {MessageId}. Delivery attempt: {DeliveryAttempt}/{MaxDeliveryAttempts}",
                        message.MessageId,
                        deliveryAttempt,
                        MaxDeliveryAttempts);

                    try
                    {
                        if (deliveryAttempt >= MaxDeliveryAttempts)
                        {
                            var deadLetterMessage = new DeadLetterMessage
                            {
                                OriginalTopic = ReservationFailed_Topic,
                                SubscriptionName = Orders_ReservationFailed_Subscription,
                                MessageId = messageId,
                                Payload = message.Value(),
                                ErrorMessage = ex.Message,
                                FailedAt = DateTime.UtcNow
                            };

                            await deadLetterProducer.Send(
                                JsonSerializer.Serialize(deadLetterMessage),
                                cancellationToken);

                            await consumer.Acknowledge(
                                message,
                                cancellationToken);

                            _redeliveryCount.Remove(messageId);

                            _logger.LogError(
                            "Message {MessageId} moved to DLQ {DlqTopic}",
                            message.MessageId,
                            Orders_DeadLetterTopic);
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
                            "Failed to recover message {MessageId} after processing failure",
                            message.MessageId);
                    }
                }
            }
        }
    }
}
