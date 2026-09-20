using Inventory.Models;
using Inventory.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryService _inventoryService;

        public InventoryController(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet("stock")]
        public async Task<IActionResult> GetStockItem(CancellationToken cancellationToken)
        {
            try
            {
                var ressult = await _inventoryService.GetStockItemAsync(cancellationToken);
                return Ok(ressult);
            }
            catch(Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("stock/{sku}/adjust")]
        public async Task<IActionResult> AdjustStockItem([FromRoute] string sku, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _inventoryService.AdjustStockItemAsync(sku, request, cancellationToken));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
