namespace OrderFlow.Client.Models.Orders
{
    public sealed record CreateOrderResponse(
        Guid OrderId,
        string CorrelationId,
        string Status);
}
