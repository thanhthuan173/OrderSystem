namespace Contracts.Events
{
    public abstract record EventBase
    {
        public Guid EventId { get; init; }
        public Guid OrderId { get; init; }
        public DateTime Timestamp { get; init; }

        protected EventBase(
            Guid eventId,
            Guid orderId,
            DateTime timestamp)
        {
            EventId = eventId;
            OrderId = orderId;
            Timestamp = timestamp;
        }
    }
}
