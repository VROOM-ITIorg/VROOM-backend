using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    namespace VROOM.Repositories
    {
        public class RiderRepository
        {
            private readonly MyDbContext _context;
            private readonly DbSet<Rider> _riders;

            public RiderRepository(MyDbContext context)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _riders = context.Set<Rider>();
            }

            public async Task<Rider> GetByIdAsync(int id)
            {
                return await _riders
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == id);
            }

            public async Task<Rider> AddAsync(Rider rider)
            {
                await _riders.AddAsync(rider);
                await _context.SaveChangesAsync();
                return rider;
            }

            public async Task UpdateAsync(int id, Rider rider)
            {
                var existingRider = await GetByIdAsync(id);
                if (existingRider == null)
                    throw new KeyNotFoundException($"Rider with ID {id} not found.");

                _context.Entry(existingRider).CurrentValues.SetValues(rider);
                await _context.SaveChangesAsync();
            }
        }
    }
}