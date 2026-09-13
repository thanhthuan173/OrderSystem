using Contracts.Events;
using DotPulsar.Abstractions;
using Inventory.Data;
using Messaging;
using System.Text.Json;

namespace Inventory.Messaging
{
    public class InventoryOrderPlacedConsumer : PulsarConsumerBackgroundService
    {
        private readonly InventoryDbContext _db;

        protected override string Topic =>
            PulsarTopics.OrderPlaced;

        protected override string SubscriptionName =>
            "inventory-order-placed";

        public InventoryOrderPlacedConsumer(
            IPulsarClient client,
            InventoryDbContext db,
            ILogger<InventoryOrderPlacedConsumer> logger)
            : base(client, logger)
        {
            _db = db;
        }

        protected override async Task HandleMessageAsync(
            string payload,
            CancellationToken cancellationToken)
        {
            var @event =
                JsonSerializer.Deserialize<OrderPlacedEvent>(
                    payload);

            if (@event == null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize OrderPlacedEvent.");
            }

            Console.WriteLine(
                $"Inventory received OrderPlaced: {@event.OrderId}");

            await Task.CompletedTask;
        }
    }
}
