namespace OrderFlow.Client.Models.Orders
{
    public sealed record GetOrderResponse(
        Guid OrderId,
        string CustomerId,
        string Status,
        decimal TotalAmount,
        bool ReservationCompleted,
        bool PaymentCompleted,
        List<OrderLineResponse> Lines,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record OrderLineResponse(
        string Sku,
        int Quantity,
        decimal UnitPrice);
}
