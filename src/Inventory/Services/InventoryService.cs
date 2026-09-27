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
            if (await IsProcessedAsync(@event.EventId, cancellationToken))
            {
                return;
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var sortedLines = @event.Lines
                    .OrderBy(l=>l.Sku)
                    .ToList();

                var stocks = new Dictionary<string, StockItem>();
                string? failureReason = null;

                foreach(var line in sortedLines)
                {
                    var stock = await _db.StockItems
                        .FromSqlInterpolated($"""
                            SELECT *
                            FROM stock_items
                            WHERE sku = {line.Sku}
                            FOR UPDATE
                            """)
                        .SingleOrDefaultAsync(cancellationToken);

                    if(stock is null)
                    {
                        failureReason= $"SKU {line.Sku} does not exist";
                        break;
                    }

                    var available = stock.QuantityOnHand - stock.QuantityReserved;
                    if (available < line.Quantity)
                    {
                        failureReason = $"Not enough stock for SKU {line.Sku}";
                        break;
                    }

                    stocks[line.Sku] = stock;
                }

                EventBase reservationResult;
                string topic;

                if (failureReason is not null)
                {
                    reservationResult = new ReservationFailedEvent(
                        Guid.NewGuid(),
                        @event.OrderId,
                        failureReason);

                    topic = ReservationFailed_Topic;
                }
                else
                {
                    foreach(var line in sortedLines)
                    {
                        var stock = stocks[line.Sku];

                        stock.QuantityReserved += line.Quantity;

                        _db.Reservations.Add(new Reservation
                        {
                            OrderId = @event.OrderId,
                            Sku = stock.Sku,
                            Quantity = line.Quantity,
                            Status = ReservationStatus.Active,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    topic = ReservationSucceeded_Topic;

                    reservationResult = new ReservationSucceededEvent(
                        Guid.NewGuid(),
                        @event.OrderId,
                        @event.Lines);
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
                    Payload = JsonSerializer.Serialize(
                        reservationResult,
                        reservationResult.GetType()),
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task HandlePaymentFailedAsync(PaymentFailedEvent @event, CancellationToken cancellationToken)
        {
            var reservations = await _db.Reservations
                .Where(r => r.OrderId == @event.OrderId)
                .ToListAsync(cancellationToken);

            foreach (var reservation in reservations)
            {
                var stockItem = await _db.StockItems
                                        .SingleAsync(s => s.Sku == reservation.Sku, cancellationToken);

                stockItem.QuantityReserved -= reservation.Quantity;
                reservation.Status = ReservationStatus.Released;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task HandlePaymentSucceededAsync(PaymentSucceededEvent @event, CancellationToken cancellationToken)
        {
            if(await IsProcessedAsync(@event.EventId, cancellationToken))
            {
                return;
            }

            var reservations = await _db.Reservations
                .Where(r =>
                    r.OrderId == @event.OrderId &&
                    r.Status == ReservationStatus.Active)
                .ToListAsync(cancellationToken);

            foreach (var reservation in reservations)
            {
                var stockItem = await _db.StockItems
                                        .SingleAsync(s => s.Sku == reservation.Sku, cancellationToken);

                stockItem.QuantityOnHand -= reservation.Quantity;
                stockItem.QuantityReserved -= reservation.Quantity;
                reservation.Status = ReservationStatus.Consumed;
            }

            _db.InboxMessages.Add(new InboxMessage
            {
                EventId = @event.EventId,
                ProcessedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken)
        {
            return await _db.InboxMessages.AnyAsync(i => i.EventId == eventId, cancellationToken);
        }
    }
}
