// Order.cs 
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;



namespace VROOM.Models
{

    public class Order
    {
        public int Id { get; set; }
        public string CustomerID { get; set; }
        public string RiderID { get; set; }
        public string ItemsType { get; set; }
        public string Title { get; set; }
        public OrderStateEnum State { get; set; } = OrderStateEnum.Created;
        public bool IsBreakable { get; set; }

        public string Notes { get; set; }
        public string Details { get; set; }
        public float Weight { get; set; }
        public OrderPriorityEnum OrderPriority { get; set; } = OrderPriorityEnum.Standard;
        public CustomerPriorityEnum CustomerPriority { get; set; } = CustomerPriorityEnum.FirstTime;

        public decimal OrderPrice { get; set; }
        public decimal DeliveryPrice { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;

        public virtual Customer Customer { get; set; }
        public virtual Rider Rider { get; set; }
        public virtual Payment Payment { get; set; }
        public virtual OrderRoute OrderRoute { get; set; }
        public virtual OrderRider OrderRider { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }
}