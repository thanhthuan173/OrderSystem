using System.Text.Json;
using Contracts.Events;
using Inventory.Data;
using Inventory.Data.Entities;
using Inventory.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services
{
    public class InventoryService
    {
        private const string ReservationFailed_Topic = "persistent://public/default/reservation-failed";
        private const string ReservationSucceeded_Topic = "persistent://public/default/reservation-succeeded";

        private readonly InventoryDbContext _db;

        public InventoryService(InventoryDbContext db)
        {
            _db = db;
        }

        public async Task<List<GetStockItemResponse>> GetStockItemAsync(CancellationToken cancellationToken)
        {
            var stockItems = await _db.StockItems.ToListAsync(cancellationToken);

            var items = new List<GetStockItemResponse>();
            foreach(var item in  stockItems)
            {
                items.Add(new GetStockItemResponse(
                    item.Sku,
                    item.QuantityOnHand,
                    item.QuantityReserved,
                    item.QuantityOnHand - item.QuantityReserved));
            }

            return items;
        }

        public async Task<GetStockItemResponse> AdjustStockItemAsync(
            string sku, 
            AdjustStockRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Quantity <= 0)
            {
                throw new Exception("Quantity must greater than 0");
            }

            var item = await _db.StockItems
                .FirstOrDefaultAsync(x => x.Sku == sku, cancellationToken);

            if (item == null)
            {
                item = new StockItem
                {
                    Sku = sku,
                    QuantityOnHand = request.Quantity
                };
                _db.StockItems.Add(item);
            }
            else
            {
                item.QuantityOnHand += request.Quantity;
            }

            await _db.SaveChangesAsync();

            return new GetStockItemResponse(
                sku,
                item!.QuantityOnHand,
                item.QuantityReserved,
                item.QuantityOnHand - item.QuantityReserved);
        }

        public async Task HandleOrderPlacedAsync(
            OrderPlacedEvent @event,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var alreadyProcessed = await _db.InboxMessages
                .AnyAsync(i => i.EventId == @event.EventId, cancellationToken);

            if (alreadyProcessed)
            {
                return;
            }

            EventBase reservationResult;
            string topic;

            string? failureReason = await ValidateLines(@event.Lines, cancellationToken);

            if(failureReason is not null)
            {
                reservationResult = new ReservationFailedEvent(
                    Guid.NewGuid(),
                    @event.OrderId,
                    failureReason);

                topic = ReservationFailed_Topic;
            }
            else
            {
                foreach ( var line in @event.Lines )
                {
                    var stock = await _db.StockItems
                        .FirstOrDefaultAsync(s => s.Sku == line.Sku, cancellationToken);

                    stock!.QuantityReserved += line.Quantity;

                    _db.Reservations.Add(new Reservation
                    {
                        OrderId = @event.OrderId,
                        Sku = line.Sku,
                        Quantity = line.Quantity,
                        Status = ReservationStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                    });
                }

                reservationResult = new ReservationSucceededEvent(
                    Guid.NewGuid(),
                    @event.OrderId,
                    @event.Lines);

                topic= ReservationSucceeded_Topic;
            }

            _db.InboxMessages.Add(new InboxMessage
            {
                EventId = @event.EventId,
                ProcessedAt = DateTime.UtcNow
            });

            _db.OutboxMessages.Add(new OutboxMessage
            {
                EventId = reservationResult.EventId,
                Topic = topic,
                Payload = JsonSerializer.Serialize(reservationResult),
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }

        public async Task HandlePaymentFailedAsync(PaymentFailedEvent @event, CancellationToken cancellationToken)
        {

        }

        public async Task HandlePaymentSucceededAsync(PaymentSucceededEvent @event, CancellationToken cancellationToken)
        {

        }

        private async Task<string?> ValidateLines(List<OrderLineContract> lines,CancellationToken cancellationToken)
        {
            foreach(var line in lines)
            {
                var stock = await _db.StockItems
                    .FirstOrDefaultAsync(s => s.Sku == line.Sku, cancellationToken);

                if(stock == null)
                {
                    return $"SKU {line.Sku} does not exists";
                }

                var available = stock.QuantityOnHand - stock.QuantityReserved;

                if (available < line.Quantity)
                {
                    return $"Not enough stock for SKU {line.Sku}";
                }
            }

            return null;
        }
    }
}
