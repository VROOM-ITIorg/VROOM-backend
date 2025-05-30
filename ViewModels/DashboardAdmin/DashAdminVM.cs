using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Models;

namespace ViewModels
{

        public class AdminDashboardViewModel
        {
            public int TotalOrders { get; set; }
            public List<OrderStatusCount> OrderStatusCounts { get; set; }
            public List<OrderByDate> OrdersByDate { get; set; }
            public decimal TotalRevenue { get; set; }
            public int TotalShipments { get; set; }
            public double? AvgDeliveryTimeHours { get; set; }
            public int ActiveRiders { get; set; }
            public List<float> RiderRatings { get; set; }
            public int TotalCustomers { get; set; }
            public List<TopRider> TopRiders { get; set; }
            public double OnTimeDeliveryRate { get; set; }
            public List<ZoneOrderCount> TopZones { get; set; }
            public int NewCustomers { get; set; }
            public int TotalIssues { get; set; }

            // إحصائيات جديدة
            public List<OrderPriorityCount> OrderPriorityCounts { get; set; } // توزيع الطلبات حسب الأولوية
            public double CancellationRate { get; set; } // نسبة الإلغاء
            public List<BusinessOwnerPerformance> BusinessOwnerPerformances { get; set; } // أداء الـ Business Owners
            public double? AverageOrderWeight { get; set; } // متوسط وزن الطلبات
        }

        public class OrderStatusCount
        {
            public OrderStateEnum Status { get; set; }
            public int Count { get; set; }
        }

        public class OrderByDate
        {
            public DateTime Date { get; set; }
            public int Count { get; set; }
        }

        public class TopRider
        {
            public string RiderId { get; set; }
            public string RiderName { get; set; }
            public int OrdersDelivered { get; set; }
            public float Rating { get; set; }
        }

        public class ZoneOrderCount
        {
            public ZoneEnum Zone { get; set; }
            public int OrderCount { get; set; }
        }

        public class OrderPriorityCount
        {
            public OrderPriorityEnum Priority { get; set; }
            public int Count { get; set; }
        }

        public class BusinessOwnerPerformance
        {
            public string BusinessOwnerId { get; set; }
            public string BusinessName { get; set; }
            public int TotalOrders { get; set; }
        }
    }


