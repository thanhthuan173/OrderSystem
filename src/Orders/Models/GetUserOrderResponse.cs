namespace Orders.Models
{
    public sealed record GetUserOrderResponse(
        Guid id,
        string Status,
        decimal Total,
        DateTime CreatedAt);
}
