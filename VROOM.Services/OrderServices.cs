using System.Threading.Tasks;
using ViewModels.User;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Repository;
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
                BusinessOwner = o.Rider.BusinessOwner.User.Name,
                RiderName = o.Rider.User?.Name,
                CustomerName = o.Customer.User.Name,
                Priority = o.OrderPriority.ToString(),
                Details = o.Details,
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
        private NotificationService notificationService;
        private CustomerServices customerService;

        public OrderService(OrderRepository _orderRepository, NotificationService _notificationService, CustomerServices _customerService)
        {
            orderRepository = _orderRepository;
            notificationService = _notificationService;
            customerService = _customerService;
        }

        public async Task CreateOrder(OrderCreateViewModel orderVM )
        {
            // We will check if the customer is exists 

            var customer = await customerService.CheckForCustomer(new CustomerAddViewModel { Username = orderVM.CustomerUsername,Name= orderVM.CustomerUsername,PhoneNumber= orderVM.CustomerPhoneNumber, BussnisOwnerId = orderVM.BusinessID});

            var order = new Order
            {
                CustomerID = customer.UserID,
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
            await notificationService.SendOrderStatusUpdateAsync(order.CustomerID, "New Order Created", order.Id,"Success");
            await notificationService.NotifyRiderOfNewOrderAsync(order.RiderID, order.Title, order.Id, "Success");

            
        }


        public async Task CreateOrder(OrderCreateViewModel orderVM, string BussinsId)
        {
            // We will check if the customer is exists 

            var customer = await customerService.CheckForCustomer(new CustomerAddViewModel { Username = orderVM.CustomerUsername, Name = orderVM.CustomerUsername, PhoneNumber = orderVM.CustomerPhoneNumber, BussnisOwnerId = BussinsId });

            var order = new Order
            {
                CustomerID = customer.UserID,
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
            await notificationService.SendOrderStatusUpdateAsync(order.CustomerID, "New Order Created", order.Id, "Success");
            await notificationService.NotifyRiderOfNewOrderAsync(order.RiderID, order.Title, order.Id, "Success");


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
                BusinessOwner = order.Rider.BusinessOwner.User.Name,
                RiderName = order.Rider.User.Name,
                CustomerName = order.Customer.User.Name,
                Priority = order.OrderPriority.ToString(),
                Details = order.Details,
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

        // update Order Status
        public async Task<Order> UpdateOrderState(int orderID , OrderStateEnum orderState)
        {
            Order order = await orderRepository.GetAsync(orderID);
            if (order == null || order.IsDeleted) return null;

            order.State = orderState;

            orderRepository.Update(order);
            orderRepository.CustomSaveChanges();

            return order;

        }

        // cron jobs
        public async Task TrackOrdersAsync()
        {
            try
            {
                var orders =  orderRepository.GetOrdersByStatusAsync(OrderStateEnum.Pending).Result;
                foreach (var order in orders)
                {
                    if (order.ModifiedAt.HasValue && order.ModifiedAt.Value.AddMinutes(30) <= DateTime.UtcNow)
                    {
                        await UpdateOrderState(order.Id, OrderStateEnum.Shipped);
                        // Here we will send notification to the bussiness owner tell him that the oreder been too long in pending state
                        Console.WriteLine($"Order {order.Id} updated to Shipped.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error tracking orders: {ex.Message}");
            }
        }

        // filter by order status


    }
}
