using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using VROOM.Models;

namespace VROOM.ViewModels
{
    public class AdminCreateBusOnwerVM : UserProfile
    {
        [Required(ErrorMessage = "Owner name is required")]
        [Display(Name = "Owner Name")]
        [StringLength(100, ErrorMessage = "Owner name cannot exceed 100 characters")]
        public string OwnerName { get; set; }

        [Required(ErrorMessage = "Business name is required")]
        [Display(Name = "Business Name")]
        [StringLength(100, ErrorMessage = "Business name cannot exceed 100 characters")]
        public string BusinessName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Business type is required")]
        [Display(Name = "Business Type")]
        public int BusinessTypeId { get; set; }

        [Display(Name = "Address")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get; set; }

        [Display(Name = "Business Logo")]
        public IFormFile? ProfilePicture { get; init; }
        public string? ImagePath { get; set; }

        [Display(Name = "Subscription Type")]
        public SubscriptionTypeEnum SubscriptionType { get; set; } = SubscriptionTypeEnum.None;

        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
    }

    public record AdminEditBusOwnerVM : UserProfile
    {
        public string? UserID { get; init; }

        [Required(ErrorMessage = "Owner name is required")]
        [Display(Name = "Owner Name")]
        [StringLength(100, ErrorMessage = "Owner name cannot exceed 100 characters")]
        public string OwnerName { get; set; }

        [Required(ErrorMessage = "Business name is required")]
        [Display(Name = "Business Name")]
        [StringLength(100, ErrorMessage = "Business name cannot exceed 100 characters")]
        public string BusinessName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Business type is required")]
        [Display(Name = "Business Type")]
        public int BusinessTypeId { get; set; }

        [Display(Name = "Address")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get; set; }

        [Display(Name = "Business Logo")]
        public IFormFile? ProfilePicture { get; init; }
        public string? ImagePath { get; set; }

        // Subscription-related properties
        [Required(ErrorMessage = "Subscription Type is required")]
        [Display(Name = "Subscription Type")]
        public string SubscriptionType { get; set; }

        [Required(ErrorMessage = "Subscription Start Date is required")]
        [Display(Name = "Subscription Start Date")]
        [DataType(DataType.Date)]
        public DateTime SubscriptionStartDate { get; set; }

        [Required(ErrorMessage = "Subscription End Date is required")]
        [Display(Name = "Subscription End Date")]
        [DataType(DataType.Date)]
        public DateTime SubscriptionEndDate { get; set; }

        [Display(Name = "Subscription Status")]
        public bool IsSubscriptionActive { get; set; }
    }


    public class AdminBusOwnerDetialsVM
    {

        public string UserID { get; init; }
        public string OwnerName { get; set; }
        public string BusinessName { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Address { get; set; }
        public string? ImagePath { get; set; }

        public string SubscriptionName { get; set; }
        public DateTime SubscriptionExpiryDate { get; set; }

    }

    public class DashboardStatsDto
    {
        public List<OrderStatusCount> OrdersByStatus { get; set; }
        public List<MonthlyRevenue> MonthlyRevenues { get; set; }
        public List<RiderPerformance> RiderPerformances { get; set; }
        public List<OrderPriorityCount> OrderPriorities { get; set; }
        public List<ZoneOrderCount> TopZones { get; set; } // New
        public List<ShipmentStatusCount> ShipmentStatuses { get; set; } // New
        public double AverageShipmentDurationHours { get; set; } // New
        public List<ItemTypeCount> OrdersByItemType { get; set; } // New
        public List<CustomerPriorityCount> CustomerPriorities { get; set; } // New
        public List<IssueTypeCount> IssuesByType { get; set; } // New
    }

    public class OrderStatusCount
    {
        public string Status { get; set; }
        public int OrderCount { get; set; }
    }

    public class MonthlyRevenue
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RiderPerformance
    {
        public string RiderName { get; set; }
        public float AverageRating { get; set; }
        public int OrdersHandled { get; set; }
    }

    public class OrderPriorityCount
    {
        public string Priority { get; set; }
        public int OrderCount { get; set; }
    }

    public class ZoneOrderCount
    {
        public string Zone { get; set; }
        public int OrderCount { get; set; }
    }

    public class ShipmentStatusCount
    {
        public string Status { get; set; }
        public int ShipmentCount { get; set; }
    }

    public class ItemTypeCount
    {
        public string ItemType { get; set; }
        public int OrderCount { get; set; }
    }

    public class CustomerPriorityCount
    {
        public string Priority { get; set; }
        public int OrderCount { get; set; }
    }

    public class IssueTypeCount
    {
        public string Type { get; set; }
        public int IssueCount { get; set; }
    }

}
