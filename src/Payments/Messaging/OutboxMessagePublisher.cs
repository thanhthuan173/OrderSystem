using System.Text.Json;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.EntityFrameworkCore;
using Payments.Data;

namespace Payments.Messaging
{
    public sealed class OutboxMessagePublisher : BackgroundService
    {
        private const int MaxBatchSize = 100;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPulsarClient _pulsarClient;
        private readonly ILogger<OutboxMessagePublisher> _logger;

        private readonly Dictionary<string, IProducer<string>> _producers = [];

        public OutboxMessagePublisher(
            IServiceScopeFactory scopeFactory,
            IPulsarClient pulsarClient,
            ILogger<OutboxMessagePublisher> logger)
        {
            _scopeFactory = scopeFactory;
            _pulsarClient = pulsarClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await PublishAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {

                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        ("An error occured while publishing outbox messages"));
                }

                await Task.Delay(TimeSpan.FromSeconds(6), cancellationToken);
            }
        }

        private async Task PublishAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

            var messages = await db.OutboxMessages
                .Where(o => o.PublishedAt == null)
                .OrderBy(o => o.Id)
                .Take(MaxBatchSize)
                .ToListAsync(cancellationToken);

            foreach(var message in messages)
            {
                var producer = GetOrCreateProducer(message.Topic);

                var orderId = GetOrderId(message.Payload);

                var metadata = new MessageMetadata
                {
                    Key = orderId.ToString()
                };

                await producer.Send(
                    metadata,
                    message.Payload,
                    cancellationToken);

                message.PublishedAt = DateTime.UtcNow;

                await db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Published Outbox event {EventId} to {Topic} for order {OrderId} at {PublishedAt}",
                    message.EventId,
                    message.Topic,
                    orderId,
                    message.PublishedAt);
            }
        }

        private IProducer<string> GetOrCreateProducer(string topic)
        {
            if(_producers.TryGetValue(topic,out var existingProducer))
            {
                return existingProducer;
            }

            var producer = _pulsarClient
                .NewProducer(Schema.String)
                .Topic(topic)
                .Create();

            _producers[topic] = producer;

            return producer;
        }

        private Guid GetOrderId(string payload)
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if(!root.TryGetProperty("orderId", out var orderIdProperty) &&
                !root.TryGetProperty("OrderId", out orderIdProperty))
            {
                throw new InvalidOperationException("Outbox payload does not contain orderId");
            }

            if(!orderIdProperty.TryGetGuid(out var orderId))
            {
                throw new InvalidOperationException("Invalid orderId");
            }

            return orderId;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);

            foreach(var producer in _producers.Values)
            {
                await producer.DisposeAsync();
            }

            _producers.Clear();
        }
    }
}
