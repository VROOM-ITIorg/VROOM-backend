using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    public class NotificationRepository : BaseRepository<Notification>
    {
        public NotificationRepository(VroomDbContext context) : base(context) { }

        public async Task<List<Notification>> GetAllNotificationsAsync()
        {
            return await GetList(n => !n.IsDeleted).ToListAsync();
        }

        public async Task<Notification> GetNotificationByIdAsync(int id)
        {
            return await GetAsync(id);
        }

        //public async Task AddNotificationAsync(Notification notification)
        //{
        //    await AddAsync(notification);
        //    await CustomSaveChangesAsync();
        //}

      
        //private async Task AddAsync(Notification notification)
        //{
         
        //    await context.Set<Notification>().AddAsync(notification);
           
        //    await CustomSaveChangesAsync();
        //}

        public async Task<int> UpdateNotificationAsync(Notification notification)
        {
            Update(notification);
            return await CustomSaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(int id, string modifiedBy)
        {
            var notification = await GetAsync(id);
            if (notification != null)
            {
                notification.IsDeleted = true;
                notification.ModifiedBy = modifiedBy;
                notification.ModifiedAt = DateTime.UtcNow;
                await CustomSaveChangesAsync();
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId)
        {
            return await GetList(n => n.UserID == userId && !n.IsDeleted)
                        .OrderByDescending(n => n.Date)
                        .ToListAsync();
        }

        private async Task<int> CustomSaveChangesAsync()
        {
            return await context.SaveChangesAsync();
        }
    }
}
