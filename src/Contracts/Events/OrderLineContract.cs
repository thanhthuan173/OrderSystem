namespace Contracts.Events
{
    public record OrderLineContract(
    string Sku,
    int Quantity,
    decimal UnitPrice);
}
