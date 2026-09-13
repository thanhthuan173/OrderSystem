using Inventory.Data;
using Inventory.Data.Entities;
using Inventory.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services
{
    public class InventoryService
    {
        private readonly InventoryDbContext _db;

        public InventoryService(InventoryDbContext db)
        {
            _db = db;
        }

        public async Task<List<GetStockItemResponse>> GetStockItemAsync()
        {
            var stockItems = await _db.StockItems.ToListAsync();

            var items = new List<GetStockItemResponse>();
            foreach(var item in  stockItems)
            {
                items.Add(new GetStockItemResponse(
                    item.Sku,
                    item.QuantityOnHand,
                    item.QuantityReserved,
                    item.QuantityOnHand - item.QuantityReserved));
            }

            return items;
        }

        public async Task<GetStockItemResponse> AdjustStockItemAsync(string sku, AdjustStockRequest request)
        {
            if (request.Quantity <= 0)
            {
                throw new Exception("Quantity must greater than 0");
            }

            var item = await _db.StockItems.FirstOrDefaultAsync(x => x.Sku == sku);

            if (item == null)
            {
                item = new StockItem
                {
                    Sku = sku,
                    QuantityOnHand = request.Quantity
                };
                _db.StockItems.Add(item);
            }
            else
            {
                item.QuantityOnHand += request.Quantity;
            }

            await _db.SaveChangesAsync();

            return new GetStockItemResponse(
                sku,
                item!.QuantityOnHand,
                item.QuantityReserved,
                item.QuantityOnHand - item.QuantityReserved);
        }
    }
}
