using System.Text.Json;
using Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Orders.Data;
using Orders.Data.Entities;
using Orders.Models;

namespace Orders.Services
{
    public class OrderService
    {
        private const string OrderPlacedTopic = "persistent://public/default/order-placed";

        private readonly OrdersDbContext _db;

        public OrderService(OrdersDbContext db)
        {
            _db = db;
        }

        public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken)
        {
            ValidateOrder(request);

            var orderId = Guid.NewGuid();
            var total = request.Lines.Sum(l => l.Quantity * l.UnitPrice);
            var order = new Order
            {
                Id = orderId,
                CustomerId = request.CustomerId,
                TotalAmount = total,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            foreach(var line in request.Lines)
            {
                _db.OrderLines.Add(new OrderLine
                {
                    OrderId = orderId,
                    Sku = line.Sku,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                });
            }
            _db.OrderSagaStates.Add(new OrderSagaState
            {
                OrderId = orderId,
                ReservationCompleted = false,
                PaymentCompleted = false,
                LastProcessedEventId = null
            });

            var orderPlacedEvent = new OrderPlacedEvent(
                eventId: Guid.NewGuid(),
                orderId: orderId,
                customerId: request.CustomerId,
                lines: request.Lines
                    .Select(line => new OrderLineContract(
                        line.Sku,
                        line.Quantity,
                        line.UnitPrice))
                    .ToList(),
                totalAmount: total);
            _db.OutboxMessages.Add(new OutboxMessage
            {
                EventId = orderPlacedEvent.EventId,
                Topic = OrderPlacedTopic,
                Payload = JsonSerializer.Serialize(orderPlacedEvent),
                CreatedAt = DateTime.UtcNow,
                PublishedAt = null
            });

            await _db.SaveChangesAsync(cancellationToken);

            return new CreateOrderResponse(
                orderId,
                orderId.ToString(), 
                order.Status.ToString());
        }

        public async Task<GetOrderResponse> GetOrderAsync(Guid id, CancellationToken cancellationToken)
        {
            var order = await _db.Orders
                .Include(o => o.SagaState)
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                ?? throw new Exception("Order not found");

            var lines = new List<GetOrderLineResponse>();
            foreach(var line in order.Lines)
            {
                lines.Add(new GetOrderLineResponse(
                    line.Sku, 
                    line.Quantity,
                    line.UnitPrice));
            }

            return new GetOrderResponse(
                order.Id,
                order.CustomerId,
                order.Status.ToString(),
                order.TotalAmount,
                order.SagaState.ReservationCompleted,
                order.SagaState.PaymentCompleted,
                lines,
                order.CreatedAt,
                order.UpdatedAt);
        }

        public async Task<IEnumerable<GetUserOrderResponse>> GetUserOrdersAsync(string customerId, CancellationToken cancellationToken)
        {
            var userOrders =  await _db.Orders
                .Where(o=>o.CustomerId==customerId)
                .ToListAsync(cancellationToken);

            var orders = new List<GetUserOrderResponse>();
            foreach(var order in userOrders)
            {
                orders.Add(new GetUserOrderResponse(
                    order.Id,
                    order.Status.ToString(),
                    order.TotalAmount,
                    order.CreatedAt));
            }

            return orders;
        }

        public async Task HandleReservationAsync(
            EventBase @event,
            CancellationToken cancellationToken,
            bool IsReservationFailed)
        {
            await using var transaction = await _db.Database
                .BeginTransactionAsync(cancellationToken);

            var alreadyProcessed = await _db.InboxMessages
                .AnyAsync(i=>i.EventId == @event.EventId, cancellationToken);

            if(alreadyProcessed)
            {
                return;
            }

            var order = await _db.Orders
                .Include(o=>o.SagaState)
                .SingleAsync(o => o.Id == @event.OrderId, cancellationToken);

            if (IsReservationFailed)
            {
                order.Status = OrderStatus.Cancelled;
            }
            else
            {
                order.Status = OrderStatus.Charging;
            }
            
            order.UpdatedAt = DateTime.UtcNow;
            order.SagaState.ReservationCompleted = true;

            _db.InboxMessages.Add(new InboxMessage
            {
                EventId = @event.EventId,
                ProcessedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }

        public async Task HandlePaymentFailedAsync(EventBase @event, CancellationToken cancellationToken)
        {

        }

        public async Task HandlePaymentSucceededAsync(EventBase @event, CancellationToken cancellationToken)
        {

        }

        private async Task UpdateStatus()
        {

        }

        private void ValidateOrder(CreateOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerId))
            {
                throw new BadHttpRequestException("Invalid customer");
            }

            if (!(request.Lines.Count > 0))
            {
                throw new BadHttpRequestException("Order must have at least one line");
            }

            ValidateOrderLine(request.Lines);
        }

        private void ValidateOrderLine(IEnumerable<CreateOrderLineRequest> lines)
        {
            if (lines.Any(l => string.IsNullOrWhiteSpace(l.Sku) || 
                                l.Quantity <= 0 || 
                                l.UnitPrice < 0))
            {
                throw new BadHttpRequestException("Each line must have Sku, quantity greater than 0 and positive unit price");
            }
        }
    }
}
