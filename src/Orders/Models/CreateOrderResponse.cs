namespace Orders.Models
{
    public sealed record CreateOrderResponse(
        Guid OrderId,
        string CorrelationId,
        string Status);
}
