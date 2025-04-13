using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VROOM.Models.Dtos;
using VROOM.Services;
using VROOM.ViewModels;

namespace VROOM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "BusinessOwner")]
    public class BusinessOwnerController : ControllerBase
    {
        private readonly BusinessOwnerService _businessOwnerService;

        public BusinessOwnerController(BusinessOwnerService businessOwnerService)
        {
            _businessOwnerService = businessOwnerService;
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