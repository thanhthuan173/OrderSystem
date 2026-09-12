namespace Orders.Data.Entities
{
    public sealed class Order
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public OrderSagaState SagaState { get; set; } = null!;
        public ICollection<OrderLine> Lines { get; set; } = [];
    }
}
