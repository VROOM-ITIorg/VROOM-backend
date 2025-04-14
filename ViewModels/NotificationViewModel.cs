using System;

namespace VROOM.ViewModels
{
    public class NotificationViewModel
    {
        public int Id { get; set; }

        public string UserID { get; set; } //  (Customer, Rider, Admin, etc.)

        public string Message { get; set; }

        public DateTime Date { get; set; } 

        public bool IsRead { get; set; } 

        public int OrderID { get; set; } 

        public string Type { get; set; } //  (OrderUpdate, AdminAlert, RiderNotification, etc.)
    }
}

