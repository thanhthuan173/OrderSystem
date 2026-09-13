namespace Payments.Models
{
    public sealed record GetPaymentResponse(
        Guid PaymentId,
        Guid OrderId,
        decimal Amount,
        string Status,
        DateTime CreatedAt);
}
