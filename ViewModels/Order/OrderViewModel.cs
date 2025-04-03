using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Models;


namespace VROOM.ViewModels
{
    public class OrderViewModel
    {
        public int Id { get; set; }
        public string CustomerID { get; set; }
        public int RiderID { get; set; }
        public string ItemsType { get; set; }
        public string Title { get; set; }
        public bool IsBreakable { get; set; }
        public string Notes { get; set; }
        public string Details { get; set; }
        public float Weight { get; set; }
        public string Priority { get; set; }
        public OrderStateEnum State { get; set; }
        public decimal OrderPrice { get; set; }
        public decimal DeliveryPrice { get; set; }
        public DateTime Date { get; set; }
        public string RiderName { get; set; }
        public string CustomerName { get; set; }
        public string BusinessOwner { get; set; }
        public List<OrderPerformanceReportViewModel> PerformanceReports { get; set; }
    }

    public class OrderPerformanceReportViewModel
    {
        public int OrderId { get; set; }
        public int RiderId { get; set; }
        public float DeliveryTime { get; set; }
        public int CustomerRating { get; set; }
    }
}

