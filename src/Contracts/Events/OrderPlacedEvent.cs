namespace Contracts.Events
{
    public record OrderPlacedEvent : EventBase
    {
        public string CustomerId { get; init; }
        public List<OrderLineContract> Lines { get; init; }
        public decimal TotalAmount { get; init; }

        public OrderPlacedEvent(
            Guid eventId,
            Guid orderId,
            string customerId,
            List<OrderLineContract> lines,
            decimal totalAmount)
            : base(eventId, orderId)
        {
            CustomerId = customerId;
            Lines = lines;
            TotalAmount = totalAmount;
        }
    }
}
