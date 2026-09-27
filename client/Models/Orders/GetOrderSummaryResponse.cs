namespace OrderFlow.Client.Models.Orders
{
    public sealed record GetOrderSummaryResponse(
        Guid OrderId,
        string Status,
        decimal TotalAmount,
        DateTime CreatedAt);
}
