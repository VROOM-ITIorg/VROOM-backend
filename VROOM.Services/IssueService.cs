using Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Transactions;
using ViewModels;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Repository;

namespace VROOM.Services
{
    public class IssueService
    {
        private readonly IssuesRepository issuesRepository;
        private readonly RiderRepository riderRepository;
        private readonly ShipmentRepository shipmentRepository;
        private readonly ILogger<IssueService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<OwnerHub> _hubContext;

        public IssueService(
            IssuesRepository issuesRepository,
            RiderRepository riderRepository,
            ILogger<IssueService> logger,
            IHttpContextAccessor httpContextAccessor,
            ShipmentRepository shipmentRepository,
            IHubContext<OwnerHub> hubContext


         )
        {
            this.issuesRepository = issuesRepository;
            this.riderRepository = riderRepository;
            this._logger = logger;
            this._httpContextAccessor = httpContextAccessor;
            this.shipmentRepository = shipmentRepository;
            this._hubContext = hubContext;

        }

        public async Task<Result<Issues>> ReportIssue(Issues issues)
        {
            if (issues == null)
            {
                _logger.LogWarning("Issues data is null");
                return Result<Issues>.Failure("Invalid issue data");
            }

            try
            {
                // Get Rider ID from claims
                var riderId = _httpContextAccessor.HttpContext?.User?.Claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(riderId))
                {
                    _logger.LogWarning("RiderID not found in token claims");
                    return Result<Issues>.Failure("Authentication failed");
                }

                // Validate rider exists and is active
                var rider = await riderRepository.GetAsync(riderId);
                if (rider == null)
                {
                    _logger.LogWarning("Rider not found or deleted - ID: {RiderID}", riderId);
                    return Result<Issues>.Failure("Rider not found or inactive");
                }

                // Get current shipment with proper validation
                var currentShipment = await shipmentRepository
                    .GetList(s => s.RiderID == riderId &&
                                (s.ShipmentState == ShipmentStateEnum.Assigned ||
                                 s.ShipmentState == ShipmentStateEnum.InTransit) &&
                                !s.IsDeleted)
                    .OrderByDescending(s => s.startTime)
                    .FirstOrDefaultAsync();

                var reportedIssue = new Issues
                {
                    RiderID = riderId,
                    Type = issues.Type,
                    Date = DateTime.UtcNow,
                    Note = issues.Note ?? string.Empty,
                    ReportedAt = DateTime.UtcNow,
                    Severity = issues.Severity,
                    Latitude = issues.Latitude,
                    Longitude = issues.Longitude,
                    Area = issues.Area,
                    ShipmentID = currentShipment?.Id
                };

                // Save the issue first
                issuesRepository.Add(reportedIssue);
                 issuesRepository.CustomSaveChanges();

                // Notify owner after successful save
                var owner = riderRepository.GetBusinessOwnerByRiderId(riderId);
                if (owner != null)
                {
                    try
                    {
                        await _hubContext.Clients.User(owner.BusinessID.ToString())
                            .SendAsync("ReceiveIssueNotification", new IssuesViewModel
                            {
                                RiderID = riderId,
                                Note = reportedIssue.Note,
                                Severity = reportedIssue.Severity,
                                Type = reportedIssue.Type,
                                ReportedAt = reportedIssue.ReportedAt
                            });
                        _logger.LogInformation("Notification sent to OwnerID: {OwnerID} for RiderID: {RiderID}", owner.BusinessID, riderId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send SignalR notification to OwnerID: {OwnerID} for RiderID: {RiderID}", owner.BusinessID, riderId);
                        // Notification failure shouldn't fail the whole operation
                    }
                }
                else
                {
                    _logger.LogWarning("Owner not found for RiderID: {RiderID}", riderId);
                }

                return Result<Issues>.Success(reportedIssue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting issue");
                return Result<Issues>.Failure("Failed to report issue");
            }
        }

        public async Task<VROOM.ViewModels.LocationDto> GetRiderLocationAtTime(string riderId, DateTime reportedAt)
        {
            return await issuesRepository.GetRiderLocationAtTimeAsync(riderId, reportedAt);
        }


        public async Task<Result<List<IssuesWithDetails>>> GetIssuesForBusinessOwnerRiders(string businessOwnerId)
        {
            try
            {
                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("BusinessOwner ID is null or empty");
                    return Result<List<IssuesWithDetails>>.Failure("BusinessOwner ID is required");
                }

                _logger.LogInformation("Retrieving issues for BusinessOwner: {BusinessOwnerId}", businessOwnerId);

                // Get riders with their user data
                var riders = await riderRepository
                    .GetList(r => r.BusinessID == businessOwnerId)
                    .Include(r => r.User)  // Include user data
                    .ToListAsync();

                if (!riders.Any())
                {
                    _logger.LogInformation("No riders found for BusinessOwner: {BusinessOwnerId}", businessOwnerId);
                    return Result<List<IssuesWithDetails>>.Success(new List<IssuesWithDetails>());
                }

                var riderIds = riders.Select(r => r.UserID).ToList();

                // Get issues with all required includes
                var issues = await issuesRepository
                    .GetList(i => riderIds.Contains(i.RiderID))
                    .Include(i => i.Rider)
                    .ThenInclude(r => r.User)  
                    .Include(i => i.Shipment)      // Include shipment if exists
                    .OrderByDescending(i => i.ReportedAt)
                    .ToListAsync();

                // Map to DTO with null checks
                var result = issues.Select(i => new IssuesWithDetails
                {
                    Id = i.Id,
                    RiderID = i.RiderID,
                    RiderName = i.Rider?.User?.Name ?? "Unknown Rider", 
                    Type = i.Type,
                    Date = i.Date,
                    Note = i.Note ?? string.Empty,
                    ReportedAt = i.ReportedAt,
                    Severity = i.Severity,
                    ShipmentID = i.ShipmentID,
                    ShipmentStatus = i.Shipment?.ShipmentState,
                    BusinessOwnerID = businessOwnerId,
                    RiderLocation = new LocationDto
                    {
                        Latitude = i.Latitude,
                        Longitude = i.Longitude,
                        Area = i.Area ?? "Unknown location"
                    }
                }).ToList();

                return Result<List<IssuesWithDetails>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving issues for BusinessOwner: {BusinessOwnerId}. Error: {ErrorMessage}",
                    businessOwnerId, ex.Message);
                return Result<List<IssuesWithDetails>>.Failure($"Failed to retrieve issues: {ex.Message}");
            }
        }
    }
}
