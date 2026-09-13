namespace Contracts.Events
{
    public record PaymentFailedEvent : EventBase
    {
        public string Reason { get; init; }

        public PaymentFailedEvent(
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
