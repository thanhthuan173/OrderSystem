namespace Contracts.Events
{
    public record ReservationSucceededEvent : EventBase
    {
        public List<OrderLineContract> Lines { get; init; }

        public ReservationSucceededEvent(
            Guid eventId,
            Guid orderId,
            DateTime timestamp,
            List<OrderLineContract> lines)
            : base(eventId, orderId, timestamp)
        {
            Lines = lines;
        }
    }
}
