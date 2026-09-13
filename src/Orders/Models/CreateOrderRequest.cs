namespace Orders.Models
{
    public sealed record CreateOrderRequest(
        string CustomerId, 
        ICollection<CreateOrderLineRequest> Lines);
}
