namespace Contracts.Events
{
    public record PaymentSucceededEvent : EventBase
    {
        public Guid PaymentId { get; init; }
        public decimal Amount { get; init; }

        public PaymentSucceededEvent(
            Guid eventId,
            Guid orderId,
            DateTime timestamp,
            Guid paymentId,
            decimal amount)
            : base(eventId, orderId, timestamp)
        {
            PaymentId = paymentId;
            Amount = amount;
        }
    }
}
