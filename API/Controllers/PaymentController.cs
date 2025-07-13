using Microsoft.AspNetCore.Mvc;
using VROOM.Services;


namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly PayPalService _payPalService;

        public PaymentController(PayPalService payPalService)
        {
            _payPalService = payPalService;
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var approvalUrl = await _payPalService.CreatePayment("10.00", "USD", "Test Product Payment");
            if (approvalUrl != null)
            {
                return Redirect(approvalUrl);
            }
            return BadRequest("Error creating PayPal payment.");
        }

        [HttpGet("Success")]
        public async Task<IActionResult> Success(string paymentId, string PayerID)
        {
            var success = await _payPalService.ExecutePayment(paymentId, PayerID);
            if (success)
            {
                return Ok(new { Status = "Payment Successful", PaymentId = paymentId });
            }
            return BadRequest("Payment not approved.");
        }

        [HttpGet("Cancel")]
        public IActionResult Cancel()
        {
            return BadRequest("Payment was cancelled.");
        }
    }
}