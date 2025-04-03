using VROOM.Repositories;
using VROOM.ViewModels;

namespace VROOM.Services
{
    public class OrderServices
    {
        private readonly OrderRepository orderRepository;
        private readonly RiderManager riderManager;
        public OrderServices (OrderRepository _orderRepository, RiderManager _riderManager) { 
            orderRepository = _orderRepository;
            riderManager = _riderManager;
        }

        public List<OrderViewModel> GetActiveOrder()
        {
            return orderRepository.GetActiveOrder().Select(o => new OrderViewModel
            {
                Id = o.Id,
                CustomerID = o.CustomerID,
                RiderID = o.RiderID,
                ItemsType = o.ItemsType,
                Title = o.Title,
                IsBreakable = o.IsBreakable,
                Notes = o.Notes,
                Details = o.Details,
                Weight = o.Weight,
                Priority = o.Priority,
                State = o.State,
                OrderPrice = o.OrderPrice,
                DeliveryPrice = o.DeliveryPrice,
                Date = o.Date,
                RiderName = o.Rider.User.Name,
                CustomerName = o.Customer.User.Name,
                BusinessOwner = riderManager.GetBusinessOwnerByRiderId(o.RiderID).BusinessOwner.User.Name

            }).ToList();
        }

        public List<OrderPerformanceReportViewModel> GetOrderPerformance(int id)
        {
            return orderRepository.GetOrderPerformance(id).Select(o => new OrderPerformanceReportViewModel
            {
                OrderId = o.Id,
                RiderId = o.RiderID,
                DeliveryTime = 30.5f,
                CustomerRating = 4
            }).ToList();
        }
    }
}
