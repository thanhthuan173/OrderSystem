using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messaging
{
    public abstract class PulsarConsumerBackgroundService : BackgroundService
    {
        private readonly IPulsarClient _client;
        private readonly ILogger _logger;

        protected abstract string Topic { get; }

        protected abstract string SubscriptionName { get; }

        protected PulsarConsumerBackgroundService(
            IPulsarClient client,
            ILogger logger)
        {
            _client = client;
            _logger = logger;
        }

        protected abstract Task HandleMessageAsync(
            string payload,
            CancellationToken cancellationToken);

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            await using var consumer = _client
                .NewConsumer(Schema.String)
                .Topic(Topic)
                .SubscriptionName(SubscriptionName)
                .InitialPosition(SubscriptionInitialPosition.Earliest)
                .Create();

            _logger.LogInformation(
                "Pulsar consumer started. Topic: {Topic}, Subscription: {Subscription}",
                Topic,
                SubscriptionName);

            await foreach (var message in consumer.Messages(stoppingToken))
            {
                try
                {
                    await HandleMessageAsync(
                        message.Value(),
                        stoppingToken);

                    await consumer.Acknowledge(
                        message,
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing Pulsar message. Topic: {Topic}",
                        Topic);

                    // Do not acknowledge the message.
                    // It can be redelivered by Pulsar.
                }
            }
        }
    }
}
