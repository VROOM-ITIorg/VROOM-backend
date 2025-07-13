using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Models.JobRecords;
using VROOM.Repository;

namespace VROOM.Services
{
    public class JobRecordService 
    {
        private readonly JobRecordRepository _jobRecordRepository;

        public JobRecordService(JobRecordRepository jobRecordRepository)
        {
            _jobRecordRepository = jobRecordRepository;
        }

        public async Task<bool> CheckIfJobExistsAsync(int shipmentId)
        {
            return await _jobRecordRepository.CheckIfJobExistsAsync(shipmentId);
        }

        public async Task AddJobRecordAsync(string jobId, int shipmentId, string hangfireJobId)
        {
            var jobRecord = new JobRecord
            {
                JobId = jobId,
                ShipmentId = shipmentId,
                Status = "Scheduled",
                ScheduledAt = DateTime.UtcNow,
                HangfireJobId = hangfireJobId,
                
            };
            await _jobRecordRepository.AddJobRecordAsync(jobRecord);
        }

        public async Task UpdateJobStatusAsync(string jobId, int shipmentId, string status, string errorMessage = null)
        {
            await _jobRecordRepository.UpdateJobStatusAsync(jobId, shipmentId, status, errorMessage);
        }
    }
}
