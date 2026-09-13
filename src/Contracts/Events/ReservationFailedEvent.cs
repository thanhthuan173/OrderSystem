namespace Contracts.Events
{
    public record ReservationFailedEvent : EventBase
    {
        public string Reason { get; init; }

        public ReservationFailedEvent(
            Guid eventId,
            Guid orderId,
            DateTime timestamp,
            string reason)
            : base(eventId, orderId, timestamp)
        {
            Reason = reason;
        }
    }
}
