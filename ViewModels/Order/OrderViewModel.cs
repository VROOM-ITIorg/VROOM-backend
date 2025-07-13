using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModels.Route;
using VROOM.Models;


namespace ViewModels.Order
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
        public string RiderId { get; set; }
        public float DeliveryTime { get; set; }
        public int CustomerRating { get; set; }
    }
    public class OrderCreateViewModel
    {
        // Edit the customerID to CustomerInfo like the username of the customer as it is uniqe then in the orderController 
        public string? CustomerID { get; set; }
        //public string? BusinessID { get; set; } // this will be a token 
        public string? RiderID { get; set; }
        public string? CustomerUsername { get; set; } // search for dropdown list
        public string? CustomerPhoneNumber { get; set; }
        public TimeSpan? PrepareTime { get; set; }
        public RouteLocation RouteLocation { get; set; }
        public string ItemsType { get; set; }
        public string Title { get; set; }
        public bool IsBreakable { get; set; }
        public string Notes { get; set; }
        public string Details { get; set; }
        public float Weight { get; set; }
        public OrderPriorityEnum OrderPriority { get; set; }
        public CustomerPriorityEnum CustomerPriority { get; set; }
        public decimal OrderPrice { get; set; }
        public decimal DeliveryPrice { get; set; }
    }

    public class OrderListDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public string CustomerName { get; set; }
        public string Priority { get; set; } 
        public string State { get; set; }
        public string RiderName { get; set; }
    }
    public class OrderDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string BusinessOwner { get; set; }
        public string CustomerName { get; set; }
        public string RiderName { get; set; }
        public string Priority { get; set; } // ممكن تستبدلها بـ enum لو عندك مستويات ثابتة
        public string State { get; set; }
        public bool IsBreakable { get; set; }
        public string Details { get; set; }
        public string Notes { get; set; }
        public float Weight { get; set; }
        public decimal OrderPrice { get; set; }
        public decimal DeliveryPrice { get; set; }
        public DateTime Date { get; set; }

        public int? shipmentId { get; set; }
    }
    public class ActiveOrdersViewModel
    {
        public List<OrderDetailsViewModel> Orders { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }

    public class OrderFilter
    {
        public string? State { get; set; }
        public string? CustomerName { get; set; }
        public string? RiderName { get; set; }
        public string? Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }


    public class OrderFeedbackViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string BusinessOwner { get; set; }
        public string CustomerName { get; set; }
        public string CustomerID { get; set; }
        public string RiderID { get; set; }
        public string RiderName { get; set; }
        public string Priority { get; set; } 
        public string State { get; set; }
        public bool IsBreakable { get; set; }
        public string Details { get; set; }
        public string Notes { get; set; }
        public float Weight { get; set; }
        public decimal OrderPrice { get; set; }
        public decimal DeliveryPrice { get; set; }
        public DateTime Date { get; set; }
        public string CustomerPhone { get; set; }


        public int? shipmentId { get; set; }
    }





}

