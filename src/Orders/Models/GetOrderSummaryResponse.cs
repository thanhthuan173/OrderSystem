namespace Orders.Models
{
    public sealed record GetOrderSummaryResponse(
        Guid OrderId,
        string Status,
        decimal TotalAmount,
        DateTime CreatedAt);
}
