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
        private const string ReservationFailedTopic = "persistent://public/default/reservation-failed";
        private const string ReservationSucceededTopic = "persistent://public/default/reservation-succeeded";

        private readonly InventoryDbContext _db;

        EventBase? reservationResult = null;
        string? topic = null;

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

            string? failureReason = null;

            foreach(var line in @event.Lines)
            {
                var stock = await _db.StockItems
                    .FirstOrDefaultAsync(s=>s.Sku == line.Sku, cancellationToken);

                if(stock is null)
                {
                    failureReason = $"SKU {line.Sku} does not exists";
                    break;
                }

                var available = stock.QuantityOnHand - stock.QuantityReserved;

                if (available < line.Quantity)
                {
                    failureReason = $"Not enough stock for SKU {line.Sku}";
                }

                if (failureReason is not null)
                {
                    RollbackChanges();

                    reservationResult = new ReservationFailedEvent(
                        Guid.NewGuid(),
                        @event.OrderId,
                        @event.Timestamp,
                        failureReason);
                    topic = ReservationFailedTopic;

                    _db.OutboxMessages.Add(new OutboxMessage
                    {
                        EventId = reservationResult!.EventId,
                        Topic = topic!,
                        Payload = JsonSerializer.Serialize(reservationResult),
                        CreatedAt = DateTime.UtcNow
                    });

                    await _db.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    return;
                }
                else
                {
                    topic = ReservationSucceededTopic;

                    stock.QuantityReserved += line.Quantity;

                    var reservation = new Reservation
                    {
                        OrderId = @event.OrderId,
                        Sku = line.Sku,
                        Quantity = line.Quantity,
                        Status = ReservationStatus.Active,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Reservations.Add(reservation);

                    reservationResult = new ReservationSucceededEvent(
                        Guid.NewGuid(),
                        @event.OrderId,
                        @event.Timestamp,
                        @event.Lines);
                    topic = ReservationSucceededTopic;
                }
            }

            _db.OutboxMessages.Add(new OutboxMessage
            {
                EventId = reservationResult!.EventId,
                Topic = topic!,
                Payload = JsonSerializer.Serialize(reservationResult),
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }

        private void RollbackChanges()
        {
            foreach (var entry in _db.ChangeTracker.Entries())
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.CurrentValues.SetValues(entry.OriginalValues);
                        entry.State = EntityState.Unchanged;
                        break;

                    case EntityState.Added:
                        entry.CurrentValues.SetValues(entry.OriginalValues);
                        entry.State = EntityState.Detached;
                        break;
                }
            }
        }
    }
}
