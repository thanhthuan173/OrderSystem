using Microsoft.EntityFrameworkCore;
using Payments.Data;
using Payments.Models;

namespace Payments.Services
{
    public class PaymentService
    {
        private readonly PaymentsDbContext _db;

        public PaymentService(PaymentsDbContext db)
        {
            _db = db;
        }

        public async Task<GetPaymentResponse> GetPaymentAsync(Guid orderId)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(x => x.OrderId == orderId)
                ??throw new Exception("Payment not found");

            return new GetPaymentResponse(
                payment.Id,
                payment.OrderId,
                payment.Amount,
                payment.Status.ToString(),
                payment.CreatedAt);
        }
    }
}
