using Microsoft.AspNetCore.Mvc;
using Payments.Services;

namespace Payments.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentService _paymentService;

        public PaymentsController(PaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet("{orderId:guid}")]
        public async Task<IActionResult> GetPayment([FromRoute] Guid orderId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _paymentService.GetPaymentAsync(orderId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
