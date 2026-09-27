using System.Text.Json;
using Contracts.Events;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Payments.Models;
using Payments.Services;

namespace Payments.Messaging
{
    public sealed class ReservationSucceededConsumer : BackgroundService
    {
        private const string ReservationSucceeded_Topic =
            "persistent://public/default/reservation-succeeded";
        private const string Payments_ReservationSucceeded_Subscription =
            "payments-reservation-succeeded";
        private const string Payments_DeadLetterTopic =
            "persistent://public/default/payments-dlq";

        private const int MaxDeliveryAttempts = 3;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPulsarClient _pulsarClient;
        private readonly PulsarHealthState _state;
        private readonly ILogger<ReservationSucceededConsumer> _logger;

        private readonly Dictionary<string, int> _redeliveryCount = [];

        public ReservationSucceededConsumer(
            IServiceScopeFactory scopeFactory,
            IPulsarClient pulsarClient,
            PulsarHealthState state,
            ILogger<ReservationSucceededConsumer> logger)
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
                .Topics(ReservationSucceeded_Topic)
                .SubscriptionName(Payments_ReservationSucceeded_Subscription)
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

            await using var deadLetterProducer=_pulsarClient
                .NewProducer(Schema.String)
                .Topic(Payments_DeadLetterTopic)
                .Create();

            await foreach(var message in consumer.Messages(cancellationToken))
            {
                try
                {
                    var @event = JsonSerializer.Deserialize<ReservationSucceededEvent>(message.Value())
                    ?? throw new InvalidOperationException("Could not deserialize ReservationSucceeded event.");

                    using var scope = _scopeFactory.CreateScope();
                    var paymentService = scope.ServiceProvider.GetRequiredService<PaymentService>();

                    await paymentService.HandleReservationSucceededAsync(@event, cancellationToken);

                    await consumer.Acknowledge(message, cancellationToken);
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
                                OriginalTopic = ReservationSucceeded_Topic,
                                SubscriptionName = Payments_ReservationSucceeded_Subscription,
                                MessageId = messageId,
                                Payload = message.Value(),
                                ErrorMessage = ex.Message,
                                DeliveryAttempt = deliveryAttempt,
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
                            Payments_DeadLetterTopic);
                        }
                        else
                        {
                            _redeliveryCount[messageId] = deliveryAttempt;

                            await consumer.RedeliverUnacknowledgedMessages(
                                new[] { message.MessageId },
                                cancellationToken);
                        }
                    }
                    catch(Exception redeliveryException)
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
