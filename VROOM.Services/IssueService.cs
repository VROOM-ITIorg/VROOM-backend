using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using ViewModels;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Repository;

namespace VROOM.Services
{
    public class IssueService
    {
        private readonly IssuesRepository issuesRepository;
        private readonly RiderRepository riderRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly OrderRepository orderRepository;
        private readonly ILogger<IssueService> _logger;
        public IssueService( IssuesRepository _issuesRepository, RiderRepository _riderRepository, ILogger<IssueService> logger, IHttpContextAccessor httpContextAccessor, OrderRepository _orderRepository)
        {
           issuesRepository = _issuesRepository;
            riderRepository = _riderRepository;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _orderRepository = orderRepository;

        }
        public async Task<Result<Issues>> ReportIssue(IssuesViewModel issues)
        {
           
            var riderIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("RiderID")?.Value;

            if (string.IsNullOrEmpty(riderIdClaim) || !int.TryParse(riderIdClaim, out int riderId))
            {
                _logger.LogWarning("Invalid or missing RiderID in token.");
                return Result<Issues>.Failure("Unauthorized access: Rider ID is missing.");
            }

            _logger.LogInformation("Started reporting issue for RiderID: {RiderID}", riderId);

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var rider = await riderRepository.GetAsync(riderId);

                if (rider == null)
                {
                    _logger.LogWarning("Rider not found with RiderID: {RiderID}", riderId);
                    return Result<Issues>.Failure("Rider not found");
                }

                var reportedIssue = new Issues
                {
                    RiderID = rider.UserID,
                    Note = issues.Note,
                    Severity = issues.Severity,
                    Type = issues.Type,
                };

                _logger.LogInformation("Creating issue for RiderID: {RiderID}, Severity: {Severity}, Type: {Type}",
                                        riderId, issues.Severity, issues.Type);

                issuesRepository.Add(reportedIssue);

        
                if (issues.Type == IssueTypeEnum.Crash ||
                    issues.Type == IssueTypeEnum.Police ||
                    issues.Type == IssueTypeEnum.StalledVehicle)
                {
                    _logger.LogInformation("Rider-related issue detected. Updating Rider and Order statuses.");

                    if (rider.Status == RiderStatusEnum.OnDelivery)
                    {
                        rider.Status = RiderStatusEnum.Available;
                        riderRepository.Update(rider);
                    }

                   
                    var activeOrder = await orderRepository.GetActiveConfirmedOrderByRiderIdAsync(rider.UserID);
                    if (activeOrder != null)
                    {
                        activeOrder.State = OrderStateEnum.Pending;
                        orderRepository.Update(activeOrder);
                    }
                }

                issuesRepository.CustomSaveChanges();

                scope.Complete();
                return Result<Issues>.Success(reportedIssue); 
            }
        }


    }
}
