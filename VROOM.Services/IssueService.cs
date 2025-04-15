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
        private readonly ILogger<IssueService> _logger;
        public IssueService( IssuesRepository _issuesRepository, RiderRepository _riderRepository, ILogger<IssueService> logger)
        {
           issuesRepository = _issuesRepository;
            riderRepository = _riderRepository; 
            _logger = logger;

        }
        public async Task<Result<Issues>> ReportIssue(IssuesViewModel issues)
        {
            _logger.LogInformation("Started reporting issue for RiderID: {RiderID}", issues.RiderID);
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var rider = await riderRepository.GetAsync(issues.RiderID);

                if (rider == null)
                {
                    _logger.LogWarning("Rider not found with RiderID: {RiderID}", issues.RiderID);

                    return Result<Issues>.Failure("Rider not found");
                }

                var reportedIssue = new Issues
                {
                    RiderID = issues.RiderID,
                    Note = issues.Note,
                    Severity = issues.Severity,
                    Type = issues.Type,
                };

                _logger.LogInformation("Creating issue for RiderID: {RiderID}, Severity: {Severity}, Type: {Type}",
                                                issues.RiderID, issues.Severity, issues.Type);
                issuesRepository.Add(reportedIssue);

           
                issuesRepository.CustomSaveChanges();

                scope.Complete();
                return Result<Issues>.Success(reportedIssue);
            }
        }

    }
}
