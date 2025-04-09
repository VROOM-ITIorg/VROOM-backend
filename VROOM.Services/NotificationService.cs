using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.ViewModels;

namespace VROOM.Services
{
    public class NotificationService
    {
        private readonly NotificationRepository notificationRepository;

        public NotificationService(NotificationRepository _notificationRepository)
        {
            notificationRepository = _notificationRepository;
        }

      
        public async Task SendOrderStatusUpdateAsync(string userId, string message, int orderId , string type)
        {
            var notification = new Notification
            {
                UserID = userId,
                Message = message,
                OrderID = orderId,
                Type = type,
                Date = DateTime.UtcNow,
                IsRead = false,
                IsDeleted = false
            };

            notificationRepository.Add(notification);
            notificationRepository.CustomSaveChanges();
        }

       
        public async Task NotifyRiderOfNewOrderAsync(string riderId, string orderTitle, int orderId,string type)
        {
            var message = $"New Order Assigned: {orderTitle}";
            await SendOrderStatusUpdateAsync(riderId, message, orderId,type);
        }

    
        public async Task SendAdminAlertAsync(string message)
        {
          
            var adminIds = new List<string> { "admin1", "admin2" };

            //foreach (var adminId in adminIds)
            //{
            //    await SendOrderStatusUpdateAsync(adminId, $" Admin Alert: {message}",);
            //}
        }


        public async Task<List<NotificationViewModel>> GetUserNotificationsAsync(string userId)
        {
            var notifications = await notificationRepository.GetUserNotificationsAsync(userId);

            return notifications.Select(n => new NotificationViewModel
            {
                Id = n.Id,
                Message = n.Message,
                Date = n.Date,
                IsRead = n.IsRead,
                UserID = n.UserID,
                OrderID = n.OrderID ?? 0,
                Type = n.Type
            }).ToList();
        }



        public async Task MarkAsReadAsync(int notificationId)
        {
            var notif = await notificationRepository.GetNotificationByIdAsync(notificationId);
            if (notif != null)
            {
                notif.IsRead = true;
                notif.ModifiedAt = DateTime.UtcNow;
                await notificationRepository.UpdateNotificationAsync(notif);
            }
        }
    }
}
