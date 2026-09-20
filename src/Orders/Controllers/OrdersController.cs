using Microsoft.AspNetCore.Mvc;
using Orders.Models;
using Orders.Services;

namespace Orders.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrdersController(OrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _orderService.CreateOrderAsync(request, cancellationToken);
                return Accepted(result);
            }
            catch (BadHttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetOrder([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _orderService.GetOrderAsync(id, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserOrders([FromQuery] string customerId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _orderService.GetUserOrdersAsync(customerId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
