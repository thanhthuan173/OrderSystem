namespace Orders.Models
{
    public sealed record GetOrderResponse(
        Guid OrderId,
        string CustomerId,
        string Status,
        decimal TotalAmount,
        bool ReservationCompleted,
        bool PaymentCompleted,
        ICollection<GetOrderLineResponse> Lines,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
