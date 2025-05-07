using Microsoft.AspNetCore.Mvc;
using static VROOM.Services.BusinessOwnerService;
using VROOM.Services;
using VROOM.ViewModels;
using VROOM.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "BusinessOwner")]
    public class BusinessOwnerController : Controller
    {
        private readonly BusinessOwnerService _businessOwnerService;
        private readonly UserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public BusinessOwnerController(BusinessOwnerService businessOwnerService, UserService userService, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _businessOwnerService = businessOwnerService;
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

        //[HttpPost("createRider")]
        //public async Task<IActionResult> CreateRider(RiderRegisterRequest rider)
        //{
        //    if (rider == null)
        //        return BadRequest();
        //    await _businessOwnerService.CreateRiderAsync(rider);

        //    return Ok(new { message = "Rider created successfully", rider });
        //}

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



   








        public class AssignOrderToRiderRequest
        {
            public int OrderId { get; set; }
            public string RiderId { get; set; }
        }










        //[Authorize(Roles = "Rider")]
        //[HttpPost("respondToOrder")]
        //public async Task<IActionResult> RespondToOrder([FromBody] RespondToOrderRequest request)
        //{
        //    if (request == null || request.OrderId <= 0 || string.IsNullOrEmpty(request.RiderId))
        //    {
        //        return BadRequest(new { message = "Invalid order or rider details." });
        //    }

        //    var result = await _businessOwnerService.RespondToOrderAsync(request.OrderId, request.RiderId, request.IsAccepted);

        //    if (result)
        //    {
        //        return Ok(new { message = request.IsAccepted ? "Order accepted." : "Order rejected." });
        //    }
        //    else
        //    {
        //        return BadRequest(new { message = "Unable to respond to the order. Please check the order status or rider." });
        //    }
        //}



        public class RespondToOrderRequest
        {
            public int OrderId { get; set; }
            public string RiderId { get; set; }
            public bool IsAccepted { get; set; }
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

    }
    

}
