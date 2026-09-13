namespace Inventory.Models
{
    public sealed record GetStockItemResponse(
        string Sku,
        int QuantityOnHand,
        int QuantityReserved,
        int Available);
}
