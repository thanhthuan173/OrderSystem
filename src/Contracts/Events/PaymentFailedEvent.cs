namespace Contracts.Events
{
    public record PaymentFailedEvent : EventBase
    {
        public string Reason { get; init; }

        public PaymentFailedEvent(
            Guid eventId,
            Guid orderId,
            string reason)
            : base(eventId, orderId)
        {
            Reason = reason;
        }
    }
}
