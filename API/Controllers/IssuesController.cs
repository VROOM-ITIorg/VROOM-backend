using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using VROOM.Models;
using ViewModels;
using VROOM.Services;

[ApiController]
[Route("api/issues")]
public class IssuesController : ControllerBase
{
    private readonly IssueService _issueService;
    private readonly ILogger<IssuesController> _logger;

    public IssuesController(IssueService issueService, ILogger<IssuesController> logger)
    {
        _issueService = issueService;
        _logger = logger;
    }

    [HttpPost("report")]
    public async Task<IActionResult> ReportIssue([FromBody] IssuesViewModel request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid issue report model.");
            return BadRequest(new
            {
                Status = "Error",
                Message = "Invalid request data.",
                Details = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage))
            });
        }

        try
        {
            _logger.LogInformation("Reporting issue for RiderID: {RiderID}", request.RiderID);

            var result = await _issueService.ReportIssue(new IssuesViewModel
            {
                RiderID = request.RiderID,
                Type = request.Type,
                Severity = request.Severity,
                Note = request.Note,
                ReportedAt = DateTime.UtcNow
            });

            if (!result.IsSuccess || result.Value == null)
            {
                return BadRequest(new
                {
                    Status = "Error",
                    Message = result.Error ?? "Failed to report issue."
                });
            }

            return Ok(new
            {
                Status = "Success",
                Issue = result.Value
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while reporting issue.");
            return StatusCode(500, new
            {
                Status = "Error",
                Message = "An internal server error occurred.",
                Details = ex.Message
            });
        }
    }
}
