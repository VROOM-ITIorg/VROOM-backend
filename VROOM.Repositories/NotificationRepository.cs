using System;
using System.Collections.Generic;
using System.Linq;
using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    public class NotificationManager
    {
        private readonly MyDbContext _dbContext;

        public NotificationManager(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        
        public List<Notification> GetAllNotifications()
        {
            return _dbContext.Notifications
                .Where(n => !n.IsDeleted)
                .ToList();
        }

        
        public Notification GetNotificationById(int id)
        {
            return _dbContext.Notifications
              .FirstOrDefault(n => n.Id == id && !n.IsDeleted);
        }

        
        public void AddNotification(Notification notification)
        {
            _dbContext.Notifications.Add(notification);
            _dbContext.SaveChanges();
        }

       
        public int UpdateNotification(Notification notification)
        {
            _dbContext.Notifications.Update(notification);
            return _dbContext.SaveChanges();
        }

       
        public void DeleteNotification(int id, string modifiedBy)
        {
            var notification = _dbContext.Notifications.Find(id);
            if (notification != null)
            {
                notification.IsDeleted = true;
                notification.ModifiedBy = modifiedBy;
                notification.ModifiedAt = DateTime.UtcNow;
                _dbContext.SaveChanges();
            }
        }

       
        public List<Notification> GetUserNotifications(string userId)
        {
            return _dbContext.Notifications
                .Where(n => n.UserID == userId && !n.IsDeleted)
                .OrderByDescending(n => n.Date)
                .ToList();
        }
    }
}
