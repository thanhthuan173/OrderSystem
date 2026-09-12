namespace Orders.Data.Entities
{
    public sealed class InboxMessage
    {
        public Guid EventId { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
