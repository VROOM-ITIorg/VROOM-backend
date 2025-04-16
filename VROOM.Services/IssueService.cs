using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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
        private readonly BusinessOwnerService businessOwnerService;
        private readonly OrderRepository orderRepository;
        private readonly ILogger<IssueService> _logger;

        public IssueService(
            IssuesRepository _issuesRepository,
            RiderRepository _riderRepository,
            ILogger<IssueService> logger,
            OrderRepository _orderRepository,
            BusinessOwnerService businessOwnerService)
        {
            issuesRepository = _issuesRepository;
            riderRepository = _riderRepository;
            _logger = logger;
            orderRepository = _orderRepository;
            this.businessOwnerService = businessOwnerService;
        }

        public async Task<Result<Issues>> ReportIssue(IssuesViewModel issues)
        {
            if (issues == null || issues.RiderID == null)
            {
                _logger.LogWarning("Invalid or missing RiderID in request.");
                return Result<Issues>.Failure("Invalid request: Rider ID is required.");
            }

            _logger.LogInformation("Started reporting issue for RiderID: {RiderID}", issues.RiderID);

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var rider = await riderRepository.GetAsync(issues.RiderID);
                    if (rider == null)
                    {
                        _logger.LogWarning("Rider not found with RiderID: {RiderID}", issues.RiderID);
                        return Result<Issues>.Failure("Rider not found");
                    }

                    var reportedIssue = new Issues
                    {
                        RiderID = rider.UserID,
                        Note = issues.Note,
                        Severity = issues.Severity,
                        Type = issues.Type,
                        ReportedAt = DateTime.UtcNow
                    };
                    //route issue
                    issuesRepository.Add(reportedIssue);


                    if (IsCriticalIssue(issues.Type))
                    {
                        await HandleCriticalIssue(rider, reportedIssue);
                    }

             
                     issuesRepository.CustomSaveChanges();
                    scope.Complete();

                    return Result<Issues>.Success(reportedIssue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing issue report for RiderID: {RiderID}", issues.RiderID);
                    return Result<Issues>.Failure("An error occurred while processing the issue");
                }
            }
        }

        private bool IsCriticalIssue(IssueTypeEnum issueType)
        {
            return issueType == IssueTypeEnum.Crash ||
                 //  issueType == IssueTypeEnum.Police ||
                   issueType == IssueTypeEnum.StalledVehicle;
        }

        private async Task HandleCriticalIssue(Rider rider, Issues reportedIssue)
        {
            _logger.LogInformation("Critical issue detected. Attempting rider reassignment.");
  
            if (rider.Status == RiderStatusEnum.OnDelivery)
            {
                rider.Status = RiderStatusEnum.Unavailable;

                riderRepository.Update(rider);
                _logger.LogInformation("Updated rider {RiderID} status to Available", rider.UserID);
            }


           
  
            var activeOrders = await orderRepository.GetActiveConfirmedOrdersByRiderIdAsync(rider.UserID);
            if (activeOrders == null)
            {
                _logger.LogInformation("No active rider found for order {RiderID}", rider.UserID);
                return;
            }


            foreach (var activeOrder in activeOrders) {
                activeOrder.State = OrderStateEnum.Pending;
                orderRepository.Update(activeOrder);
                orderRepository.CustomSaveChanges();
                _logger.LogInformation("Reset order {OrderID} state to Created", activeOrder.Id);


                //route start point is the same place the rider is at right now>>>

               
            }
        }

  
    }
    }