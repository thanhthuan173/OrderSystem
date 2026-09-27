namespace OrderFlow.Client.Models.Inventory
{
    public sealed record GetStockItemResponse(
        string Sku,
        int QuantityOnHand,
        int QuantityReserved,
        int Available);
}
