namespace Contracts.Events
{
    public abstract record EventBase
    {
        public Guid EventId { get; init; }
        public Guid OrderId { get; init; }

        protected EventBase(
            Guid eventId,
            Guid orderId)
        {
            EventId = eventId;
            OrderId = orderId;
        }
    }
}
