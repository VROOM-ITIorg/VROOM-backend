using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VROOM.Models.Dtos;
using VROOM.Services;

namespace API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        // POST: api/user/register

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _userService.RegisterAsync(request);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }

        // POST: api/user/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _userService.LoginAsync(request.Email, request.Password);
            return result.IsSuccess ? Ok(new { Token = result.Value }) : BadRequest(result.Error);
        }

        // POST: api/user/assign-role
        [HttpPost("assign-role")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            var result = await _userService.AssignRoleAsync(request.UserId, request.Role);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }

        // PUT: api/user/profile
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

            var result = await _userService.UpdateProfileAsync(userId, request.Name, request.ProfilePicture, request.Address);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }

        // PUT: api/user/password
        [HttpPut("password")]
        [Authorize]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

            var result = await _userService.UpdatePasswordAsync(userId, request.NewPassword);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }
    }

    public record LoginRequest(string Email, string Password);
    public record AssignRoleRequest(string UserId, string Role);
    public record UpdateProfileRequest(string Name, string? ProfilePicture, AddressDto? Address); // Changed to AddressDto
    public record UpdatePasswordRequest(string NewPassword);
}
