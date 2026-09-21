namespace Contracts.Events
{
    public record ReservationFailedEvent : EventBase
    {
        public string Reason { get; init; }

        public ReservationFailedEvent(
            Guid eventId,
            Guid orderId,
            string reason)
            : base(eventId, orderId)
        {
            Reason = reason;
        }
    }
}
