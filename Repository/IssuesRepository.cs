using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.ViewModels;

namespace VROOM.Repository
{
    public class IssuesRepository : BaseRepository<Issues>
    {

        public IssuesRepository(VroomDbContext context) : base(context) { }

        public async Task<LocationDto> GetRiderLocationAtTimeAsync(string riderId, DateTime reportedAt)
        {
            var location = await GetList(i => i.RiderID == riderId && i.ReportedAt <= reportedAt)
                .OrderByDescending(i => i.ReportedAt)
                .Select(i => new LocationDto
                {
                    Lat = i.Latitude,
                    Lang = i.Longitude,
                    Area = i.Area
                })
                .FirstOrDefaultAsync();

            return location ?? new LocationDto
            {
                Lat = 0,
                Lang = 0,
                Area = "Unknown location"
            };
        }

    }
}
