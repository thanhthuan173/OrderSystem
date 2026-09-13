namespace Contracts.Events
{
    public record ReservationSucceededEvent : EventBase
    {
        public Guid ReservationId { get; init; }
        public List<OrderLineContract> Lines { get; init; }

        public ReservationSucceededEvent(
            Guid eventId,
            Guid orderId,
            DateTime timestamp,
            Guid reservationId,
            List<OrderLineContract> lines)
            : base(eventId, orderId, timestamp)
        {
            ReservationId = reservationId;
            Lines = lines;
        }
    }
}
