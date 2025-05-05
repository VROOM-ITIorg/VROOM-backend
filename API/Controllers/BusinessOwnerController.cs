using Microsoft.AspNetCore.Mvc;
using VROOM.Services;
using VROOM.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VROOM.Repositories;
using static VROOM.Services.BusinessOwnerService;
using VROOM.ViewModels;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusinessOwnerController : Controller
    {
        private readonly BusinessOwnerService _businessOwnerService;
        private readonly BusinessOwnerRepository _businessOwnerRepository;
        private readonly UserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BusinessOwnerController(
            BusinessOwnerService businessOwnerService,
            BusinessOwnerRepository businessOwnerRepository,
            UserService userService,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _businessOwnerService = businessOwnerService;
            _businessOwnerRepository = businessOwnerRepository;
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterBusinessOwner([FromBody] BusinessOwnerRegisterRequest request)
        {
            var result = await _businessOwnerService.CreateBusinessOwnerAsync(request);
            if (!result.IsSuccess)
                return BadRequest();

            return Ok(result.Value);
        }

        [HttpPost("changePassword")]
        public async Task<IActionResult> ChangeRiderPassword(string riderId, string newPassword)
        {
            var result = await _businessOwnerService.ChangeRiderPasswordAsync(riderId, newPassword);
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }
            return Ok(result.Value);
        }

        [HttpPost("registerRider")]
        public async Task<IActionResult> RegisterRider([FromBody] RiderRegisterRequest rider)
        {
            var result = await _businessOwnerService.CreateRiderAsync(rider);
            return Ok(result);
        }

        [Authorize(Roles = "BusinessOwner")]
        [HttpPost("assignOrderManually")]
        public async Task<IActionResult> AssignOrderToRider([FromBody] AssignOrderToRiderRequest request)
        {
            if (request == null || request.OrderId <= 0 || string.IsNullOrEmpty(request.RiderId))
            {
                return BadRequest(new { message = "Invalid order or rider details." });
            }

            var success = await _businessOwnerService.AssignOrderToRiderAsync(request.OrderId, request.RiderId);
            if (!success)
            {
                return NotFound(new { message = "Unable to assign the order to the rider. Please check the details." });
            }

            return Ok(new { message = "Order successfully assigned to the rider." });
        }

        [Authorize(Roles = "BusinessOwner")]
        [HttpPost("assignOrderAutomatically")]
        public async Task<IActionResult> AssignOrderAutomatically([FromBody] AssignOrderAutomaticallyRequest request)
        {
            if (request == null || request.OrderId <= 0 || string.IsNullOrEmpty(request.BusinessOwnerId))
            {
                return BadRequest(new { message = "Invalid order or business owner details." });
            }

            var result = await _businessOwnerService.AssignOrderAutomaticallyAsync(request.BusinessOwnerId, request.OrderId);
            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        [HttpGet("riders")]
        public async Task<IActionResult> GetRiders()
        {
            var result = await _businessOwnerService.GetRiders();
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }
            return Ok(result.Value);
        }

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            var result = await _businessOwnerService.GetCustomers();
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }
            return Ok(result.Value);
        }

        public class AssignOrderToRiderRequest
        {
            public int OrderId { get; set; }
            public string RiderId { get; set; }
        }

        public class RespondToOrderRequest
        {
            public int OrderId { get; set; }
            public string RiderId { get; set; }
            public bool IsAccepted { get; set; }
        }

        public class AssignOrderAutomaticallyRequest
        {
            public string BusinessOwnerId { get; set; }
            public int OrderId { get; set; }
        }
    }
}