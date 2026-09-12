namespace Orders.Data.Entities
{
    public sealed class OrderLine
    {
        public long Id { get; set; }
        public Guid OrderId { get; set; }
        public string Sku { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public Order Order { get; set; } = null!;
    }
}
