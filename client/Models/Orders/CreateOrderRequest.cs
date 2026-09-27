namespace OrderFlow.Client.Models.Orders
{
    public sealed class CreateOrderRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public List<OrderLineRequest> Lines { get; set; } = [];
    }
    public sealed class OrderLineRequest
    {
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; } = 10.00m;
    }
}
