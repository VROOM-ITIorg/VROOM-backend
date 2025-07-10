using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Models.JobRecords;
using VROOM.Repositories;

namespace VROOM.Repository
{
    public class JobRecordRepository : BaseRepository<JobRecord>
    {

        public JobRecordRepository(VroomDbContext dbContext) : base(dbContext) { }

        public async Task<bool> CheckIfJobExistsAsync(int shipmentId)
        {
            return await context.JobRecords
                .AnyAsync(j => j.ShipmentId == shipmentId && j.Status == "Scheduled");
        }

        public async Task AddJobRecordAsync(JobRecord jobRecord)
        {
            await context.JobRecords.AddAsync(jobRecord);
            await context.SaveChangesAsync();
        }

        public async Task UpdateJobStatusAsync(string jobId, int shipmentId, string status, string errorMessage = null)
        {
            var jobRecord = await context.JobRecords
                .FirstOrDefaultAsync(j => j.JobId == jobId && j.ShipmentId == shipmentId);
            if (jobRecord != null)
            {
                jobRecord.Status = status;
                jobRecord.ErrorMessage = errorMessage;
                if (status == "Completed" || status == "Failed")
                {
                    jobRecord.CompletedAt = DateTime.UtcNow;
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
