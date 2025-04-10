using Microsoft.AspNetCore.Mvc;
using static VROOM.Services.BusinessOwnerService;
using VROOM.Services;
using VROOM.ViewModels;
using VROOM.Models;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusinessOwnerController : Controller
    {
        private readonly BusinessOwnerService _businessOwnerService;
        private readonly UserService _userService;
        public BusinessOwnerController(BusinessOwnerService businessOwnerService, UserService userService)
        {

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

        [HttpPost("createRider")]
        public async Task<IActionResult> CreateRider(RiderRegisterRequest rider)
        {
            if (rider == null)
                return BadRequest();
            await _businessOwnerService.CreateRiderAsync(rider);

            return Ok(new { message = "Rider created successfully", rider });
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
            if (request == null)
            {
                return BadRequest("Invalid data.");
            }

            var result = await _businessOwnerService.AssignOrderToRiderAsync(request.OrderId, request.RiderId, request.BusinessOwnerId);

            if (result == null)
            {
                return NotFound("Unable to assign order to rider. Please check the details.");
            }

            return Ok(result);
        }
        public class AssignOrderToRiderRequest
        {

            public int OrderId { get; set; }


            public string RiderId { get; set; }


            public string BusinessOwnerId { get; set; }
        }


    }

}
