using LinqKit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using ViewModels;
using ViewModels.Order;
using ViewModels.User;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Repository;

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
                State = o.State.ToString(),
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
        private BusinessOwnerRepository businessOwnerRepository;
        private RiderRepository riderRepository;
        private NotificationService notificationService;
        private CustomerServices customerService;
        private OrderRouteServices orderRouteServices;
        private RouteServices routeService;
        private readonly ShipmentRepository shipmentRepository;
        private readonly RouteRepository routeRepository;
        private readonly IHttpContextAccessor httpContextAccessor;
        public OrderService(
            OrderRepository _orderRepository,
            NotificationService _notificationService,
            CustomerServices _customerService,
            RouteServices _routeServices,
            RiderRepository _riderRepository,
            OrderRouteServices _orderRouteServices,
            ShipmentRepository _shipmentRepository,
            RouteRepository _routeRepository,
            IHttpContextAccessor _httpContextAccessor,
            BusinessOwnerRepository _businessOwnerRepository
            )
        {
            orderRepository = _orderRepository;
            notificationService = _notificationService;
            customerService = _customerService;
            routeService = _routeServices;
            riderRepository = _riderRepository;
            orderRouteServices = _orderRouteServices;
            shipmentRepository = _shipmentRepository;
            routeRepository = _routeRepository;
            httpContextAccessor = _httpContextAccessor;
            businessOwnerRepository = _businessOwnerRepository;
        }

        public async Task<Order> CreateOrder(OrderCreateViewModel orderVM, string BussinsId)
        {
            // We will check if the customer is exists 

            var customer = await customerService.CheckForCustomer(new CustomerAddViewModel { Username = orderVM.CustomerUsername, Name = orderVM.CustomerUsername, PhoneNumber = orderVM.CustomerPhoneNumber, BussnisOwnerId = BussinsId });

            var owner = await businessOwnerRepository.GetAsync(BussinsId);
            orderVM.RouteLocation.OriginLat = owner.User.Address.Lat;
            orderVM.RouteLocation.OriginLang = owner.User.Address.Lang;
            orderVM.RouteLocation.OriginArea = owner.User.Address.Area;

            var route = await routeService.CreateRoute(orderVM.RouteLocation);

            var order = new Order
            {
                CustomerID = customer.UserID,
                //RiderID = orderVM.RiderID,
                ItemsType = orderVM.ItemsType,
                Title = orderVM.Title,
                IsBreakable = orderVM.IsBreakable,
                Notes = orderVM.Notes,
                Details = orderVM.Details,
                Weight = orderVM.Weight,
                PrepareTime = orderVM.PrepareTime,
                OrderPriority = orderVM.OrderPriority,
                CustomerPriority = orderVM.CustomerPriority,
                OrderPrice = orderVM.OrderPrice,
                DeliveryPrice = orderVM.DeliveryPrice,
                Date = DateTime.Now,
                zone = orderVM.zone,
                BusinessID = BussinsId
            };

            orderRepository.Add(order);
            orderRepository.CustomSaveChanges();

            await orderRouteServices.CreateOrderRoute(order.Id, route.Id);

          

          //  await notificationService.SendOrderStatusUpdateAsync(order.CustomerID, "New Order Created", order.Id, "Success");
           // //await notificationService.NotifyRiderOfNewOrderAsync(order.RiderID, order.Title, order.Id, "Success");

            return order;
        }

        public async Task<object> GetOrderByIdAsync(int orderId)
        {
            var businessOwnerId = httpContextAccessor.HttpContext?.User?
                   .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var businessOwner = await businessOwnerRepository.GetAsync(businessOwnerId);
            Order order = await orderRepository.GetAsync(orderId);
            if (order == null || order.IsDeleted) return null;

            return new OrderDetailsViewModel
            {
                Id = order.Id,
                Title = order.Title,
                State = order.State.ToString(),
                BusinessOwner = businessOwner.User.Name,
                CustomerName = order.Customer.User.Name,
                Priority = order.OrderPriority.ToString(),
                Details = order.Details,
                OrderPrice = order.OrderPrice,
                DeliveryPrice = order.DeliveryPrice,
                Date = order.Date
            };
        }
        public async Task<int> GetTotalOrders(OrderFilter filter = null)
        {
            var query = orderRepository.GetList(o => o.IsDeleted != true);

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.State))
                    query = query.Where(o => o.State.ToString() == filter.State);
                if (!string.IsNullOrEmpty(filter.CustomerName))
                    query = query.Where(o => o.Customer.User.Name.Contains(filter.CustomerName));
                if (!string.IsNullOrEmpty(filter.RiderName))
                    query = query.Where(o => o.Rider.User.Name.Contains(filter.RiderName));
                if (!string.IsNullOrEmpty(filter.Priority))
                    query = query.Where(o => o.OrderPriority.ToString() == filter.Priority);
                if (filter.StartDate.HasValue)
                    query = query.Where(o => o.Date >= filter.StartDate.Value);
                if (filter.EndDate.HasValue)
                    query = query.Where(o => o.Date <= filter.EndDate.Value);
            }

            return await query.CountAsync();
        }
        public async Task<List<OrderListDetailsViewModel>> GetAllOrders(OrderFilter filter = null,int pageNumber = 1, int pageSize = 1)
        {
            var query = orderRepository.GetList(o => o.IsDeleted != true);

            // Apply filters if provided
            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.State))
                {
                    query = query.Where(o => o.State.ToString() == filter.State);
                }

                if (!string.IsNullOrEmpty(filter.CustomerName))
                {
                    query = query.Where(o => o.Customer.User.Name.Contains(filter.CustomerName));
                }

                if (!string.IsNullOrEmpty(filter.RiderName))
                {
                    query = query.Where(o => o.Rider.User.Name.Contains(filter.RiderName));
                }

                if (!string.IsNullOrEmpty(filter.Priority))
                {
                    query = query.Where(o => o.OrderPriority.ToString() == filter.Priority);
                }

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(o => o.Date >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(o => o.Date <= filter.EndDate.Value);
                }
            }

            var orders = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderListDetailsViewModel
                {
                    Id = o.Id,
                    Title = o.Title,
                    State = o.State.ToString(),
                    RiderName = o.Rider.User.Name,
                    CustomerName = o.Customer.User.Name,
                    Priority = o.OrderPriority.ToString(),
                    Date = o.Date
                })
                .ToListAsync(); // Materialize the query
            return orders;
        }
        // We need to AssignOrderToRider with two diffrenet way (Manually and Automatically)

        // Assign an Order to a Rider
        // In service section

        // Automatically
        //public async void AssignOrderToRider(int orderId , string riderID)
        //{
        //    var rider = await riderRepository.GetAsync(riderID);

        //    await UpdateOrderState(orderId, OrderStateEnum.Confirmed);



        //}

        // Calculate Total Revenue
        public decimal CalculateTotalRevenue(int orderId) => orderRepository.SumOrderRevenue(orderId);

        // update Order Status
        public async Task<Order> UpdateOrderState(int orderID, OrderStateEnum orderState, string riderId, string businessOwnerId)
        {

            Rider rider = await riderRepository.GetAsync(riderId);
            Order order = await orderRepository.GetAsync(orderID);
            if (order == null || order.IsDeleted) return null;



            order.RiderID = riderId;
            order.State = OrderStateEnum.Confirmed;
            order.ModifiedBy = businessOwnerId;
            order.ModifiedAt = DateTime.Now;
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
                var orders = orderRepository.GetOrdersByStatusAsync(OrderStateEnum.Pending).Result;
                foreach (var order in orders)
                {
                    if (order.ModifiedAt.HasValue && order.ModifiedAt.Value.AddMinutes(30) <= DateTime.UtcNow)
                    {
                        //await UpdateOrderState(order.Id, OrderStateEnum.Shipped);
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


        public async Task<ActiveOrdersViewModel> GetActiveOrdersAsync(
            string priority = null,
            string state = null,
            string customer = null,
            string rider = null,
            bool? isBreakable = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string search = null,
            int pageNumber = 1,
            int pageSize = 4,
            string sort = "title_asc")
        {
            // Validate filter inputs
            if (dateFrom.HasValue && dateTo.HasValue && dateFrom > dateTo)
                throw new ArgumentException("Date From cannot be later than Date To.");

            if (minPrice.HasValue && maxPrice.HasValue && minPrice >= maxPrice)
                throw new ArgumentException("Minimum price must be less than maximum price.");

            // Build filter predicate
            var builder = PredicateBuilder.New<Order>();
            builder = builder.And(o => !o.IsDeleted);

            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<OrderPriorityEnum>(priority, out var priorityEnum))
                builder = builder.And(o => o.OrderPriority == priorityEnum);

            if (!string.IsNullOrEmpty(state) && Enum.TryParse<OrderStateEnum>(state, out var stateEnum))
                builder = builder.And(o => o.State == stateEnum);

            if (!string.IsNullOrEmpty(customer))
                builder = builder.And(o => o.Customer.User.Name.ToLower().Contains(customer.ToLower()));

            if (!string.IsNullOrEmpty(rider))
                builder = builder.And(o => o.Rider.User.Name.ToLower().Contains(rider.ToLower()));

            if (isBreakable.HasValue)
                builder = builder.And(o => o.IsBreakable == isBreakable.Value);

            if (dateFrom.HasValue)
                builder = builder.And(o => o.Date >= dateFrom.Value);

            if (dateTo.HasValue)
                builder = builder.And(o => o.Date <= dateTo.Value);

            if (minPrice.HasValue)
                builder = builder.And(o => o.OrderPrice >= minPrice.Value);

            if (maxPrice.HasValue)
                builder = builder.And(o => o.OrderPrice <= maxPrice.Value);

            //if (!string.IsNullOrEmpty(search))
            //    builder = builder.And(o => o.Title.ToLower().Contains(search.ToLower()) || o.Details.ToLower().Contains(search.ToLower()));

            // Get paginated orders
            var (orders, totalCount) = await orderRepository.GetPaginatedOrdersAsync(
                filter: builder,
                pageSize: pageSize,
                pageNumber: pageNumber,
                sort: sort);

            // Get price range
            var (minPriceRange, maxPriceRange) = await orderRepository.GetPriceRangeAsync(
                priority, state, customer, rider, isBreakable, dateFrom, dateTo, search);

            var viewModel = new ActiveOrdersViewModel
            {
                Orders = orders.Select(o => new OrderDetailsViewModel
                {
                    Id = o.Id,
                    Title = o.Title,
                    CustomerName = o.Customer?.User.Name ?? o.CustomerID,
                    RiderName = o.Rider?.User?.Name,
                    Priority = o.OrderPriority.ToString(),
                    State = o.State.ToString(),
                    IsBreakable = o.IsBreakable,
                    Details = o.Details,
                    OrderPrice = o.OrderPrice,
                    DeliveryPrice = o.DeliveryPrice,
                    Date = o.Date,
                    //shipmentId = o.OrderRoute.Route.ShipmentID
                }).ToList(),
                MinPrice = minPriceRange,
                MaxPrice = maxPriceRange,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Total = totalCount
            };

            return viewModel;
        }
    }
}
