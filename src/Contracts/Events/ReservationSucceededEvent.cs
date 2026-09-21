namespace Contracts.Events
{
    public record ReservationSucceededEvent : EventBase
    {
        public List<OrderLineContract> Lines { get; init; }

        public ReservationSucceededEvent(
            Guid eventId,
            Guid orderId,
            List<OrderLineContract> lines)
            : base(eventId, orderId)
        {
            Lines = lines;
        }
    }
}
