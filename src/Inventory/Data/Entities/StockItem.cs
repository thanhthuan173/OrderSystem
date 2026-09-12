namespace Inventory.Data.Entities
{
    public sealed class StockItem
    {
        public string Sku { get; set; } = null!;
        public int QuantityOnHand { get; set; }
        public int QuantityReserved { get; set; }
    }
}