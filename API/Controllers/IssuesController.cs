// API/Controllers/IssuesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ViewModels;
using VROOM.Models;
using VROOM.Services;
using VROOM.ViewModels;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]  // Order fixed - ApiController should come after Route
    [Authorize]
    public class IssuesController : ControllerBase
    {
        private readonly IssueService _issueService;
        private readonly ShipmentServices _shipmentService;
        private readonly BusinessOwnerService _businessOwnerService;
        private readonly RiderService _riderService;

        public IssuesController(IssueService issueService, ShipmentServices shipmentServices, BusinessOwnerService businessOwnerService, RiderService riderService)
        {
            _issueService = issueService ?? throw new ArgumentNullException(nameof(issueService));
            _shipmentService = shipmentServices ?? throw new ArgumentNullException(nameof(shipmentServices));
            _businessOwnerService = businessOwnerService;
            _riderService = riderService;
        }

        [HttpPost("Issuereport")]  // Added explicit route
        public async Task<IActionResult> ReportingIssue([FromBody] IssueReportRequest request)
        {
            try
            {
                // Validate input
                if (request?.RiderLocation == null)
                {
                    return BadRequest("Location data is required");
                }

                // Get rider ID from JWT token
                var riderIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (riderIdClaim == null)
                {
                    return Unauthorized("RiderID not found in token");
                }

                // Get rider name from database
                var riderName = await _riderService.GetRiderNameAsync(riderIdClaim.Value);
                if (string.IsNullOrEmpty(riderName))
                {
                    return BadRequest("Rider not found");
                }

                // Get business owner ID from database
                var businessOwnerId = await _businessOwnerService.GetBusinessOwnerIdForRiderAsync(riderIdClaim.Value);
                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    return BadRequest("Business owner not found for rider");
                }

                // Create issue with server-generated fields
                var issue = new Issues
                {
                    // From request body
                    Type = request.Type,
                    Severity = request.Severity,
                    Note = request.Note,
                    Latitude = request.RiderLocation.Latitude,
                    Longitude = request.RiderLocation.Longitude,
                    Area = request.RiderLocation.Area,

                    // Server-generated fields
                    RiderID = riderIdClaim.Value,
                    Date = DateTime.UtcNow,
                    ReportedAt = DateTime.UtcNow
                };

                // Report the issue
                var result = await _issueService.ReportIssue(issue);

                if (!result.IsSuccess)
                {
                    return BadRequest(result.Error);
                }

                // Return success response
                return Ok(new IssueReportResponse
                {
                    Id = result.Value.Id,
                    RiderID = result.Value.RiderID,
                    RiderName = riderName,
                    Type = result.Value.Type,
                    Severity = result.Value.Severity,
                    Note = result.Value.Note,
                    ReportedAt = result.Value.ReportedAt,
                    ShipmentID = result.Value.ShipmentID,
                    RiderLocation = new ViewModels.LocationDto
                    {
                        Latitude = result.Value.Latitude,
                        Longitude = result.Value.Longitude,
                        Area = result.Value.Area
                    }
                });
            }
            catch (Exception ex)
            {

                return StatusCode(500, ex);
            }
        }

        [HttpGet("all")]
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> GetAllIssues()
        {
            try
            {
                var businessOwnerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    return Unauthorized("BusinessOwner ID not found in token");
                }

                var result = await _issueService.GetIssuesForBusinessOwnerRiders(businessOwnerId);

                if (!result.IsSuccess)
                {
                    return BadRequest(result.Error);
                }

                var enhancedIssues = result.Value.Select(issue => new
                {
                    issue.Id,
                    issue.RiderID,
                    RiderName = issue.RiderName ?? "Unknown Rider",
                    issue.Type,
                    issue.Date,
                    issue.Note,
                    issue.ReportedAt,
                    issue.Severity,
                    issue.ShipmentID,
                    BusinessOwnerID = businessOwnerId,
                    RiderLocation = new
                    {
                        issue.RiderLocation.Latitude,
                        issue.RiderLocation.Longitude,
                        Area = issue.RiderLocation.Area ?? "Unknown"
                    }
                }).ToList();

                return Ok(enhancedIssues);
            }
            catch (Exception ex)
            {

                return StatusCode(500, "Internal server error");
            }
        }
    }
}
