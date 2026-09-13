namespace Orders.Models
{
    public sealed record GetOrderLineResponse(
        string Sku,
        int Quantity,
        decimal UnitPrice);
}
