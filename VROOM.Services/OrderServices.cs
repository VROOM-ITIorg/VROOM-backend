using VROOM.Models;
using VROOM.Repositories;
using VROOM.ViewModels;

namespace VROOM.Services
{
    public class OrderService
    {
        //private readonly OrderRepository orderRepository;
        //private readonly RiderManager riderManager;
        //public OrderServices (OrderRepository _orderRepository, RiderManager _riderManager) { 
        //    orderRepository = _orderRepository;
        //    riderManager = _riderManager;
        //}

        public List<OrderDetailsViewModel> GetActiveOrder()
        {
            return orderRepository.GetActiveOrder().Select(o => new OrderDetailsViewModel
            {
                Id = o.Id,
                Title = o.Title,
                Notes = o.Notes,
                Weight = o.Weight,
                State = o.State,
                OrderPrice = o.OrderPrice,
                DeliveryPrice = o.DeliveryPrice,
                Date = o.Date,
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

        private OrderRepository orderRepository;

        public OrderService(OrderRepository _orderRepository)
        {
            orderRepository = _orderRepository;
        }

        public void CreateOrder(OrderCreateViewModel orderVM , out int _id)
        {
            var order = new Order
            {
                CustomerID = orderVM.CustomerID,
                RiderID = orderVM.RiderID,
                ItemsType = orderVM.ItemsType,
                Title = orderVM.Title,
                IsBreakable = orderVM.IsBreakable,
                Notes = orderVM.Notes,
                Details = orderVM.Details,
                Weight = orderVM.Weight,
                OrderPriority = orderVM.OrderPriority,
                CustomerPriority = orderVM.CustomerPriority,
                OrderPrice = orderVM.OrderPrice,
                DeliveryPrice = orderVM.DeliveryPrice,
                Date = DateTime.Now
            };

            orderRepository.Add(order);
            orderRepository.CustomSaveChanges();

            _id = order.Id;
        }

        public async Task<object> GetOrderByIdAsync(int orderId)
        {
            Order order = await orderRepository.GetAsync(orderId);
            if (order == null || order.IsDeleted) return null;

            return new OrderDetailsViewModel
            {
                Id = order.Id,
                Title = order.Title,
                State = order.State,
                Notes = order.Notes,
                Weight = order.Weight,
                OrderPrice = order.OrderPrice,
                DeliveryPrice = order.DeliveryPrice,
                Date = order.Date
            };
        }
        // We need to AssignOrderToRider with two diffrenet way (Manually and Automatically)

        // Assign an Order to a Rider
        // In service section

        // Automatically
        public void AssignOrderToRiderAuto(int orderId)
        {
            var order = orderRepository.GetAsync(orderId);

        }

        // Calculate Total Revenue
        public decimal CalculateTotalRevenue(int orderId) => orderRepository.SumOrderRevenue(orderId);

        // Check Order Status
        public string GetOrderState(int orderId)
        {
            var orderStatus = orderRepository.TrackOrder(orderId);

            return orderStatus;

        }

        // filter by order status


    }
}
