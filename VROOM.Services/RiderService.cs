using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Core.Types;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using ViewModels.Shipment;
using VROOM.Data;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Repositories;
using VROOM.Repository;
using Microsoft.AspNetCore.Http;

namespace VROOM.Services
{
    public interface IRiderService
    {
        Task<RiderLocationDto> GetRiderLastLocationAsync(string riderId);
        Task<string> GetBusinessOwnerByRiderIdAsync(string riderId);
    }

    public class RiderService : IRiderService
    {
        private readonly Dictionary<string, RiderLocationDto> _riderLocations = new();
        private readonly Dictionary<string, string> _riderToBusinessOwner = new();

        private readonly RiderRepository _riderRepository;
        private readonly OrderRouteRepository _orderRouteRepository;
        private readonly OrderRepository _orderRepository;
        private readonly ShipmentRepository _shipmentRepository;

        private readonly RouteRepository _routeRepository;
        private readonly VroomDbContext _context;
        private readonly Microsoft.EntityFrameworkCore.DbSet<Order> _orders;
        private readonly Microsoft.EntityFrameworkCore.DbSet<OrderRider> _orderRiders;
        private readonly ShipmentServices _shipmentServices;
        private readonly OrderService _orderService;

        public RiderService(RiderRepository riderRepository, VroomDbContext context, OrderRepository orderRepository, ShipmentRepository shipmentRepository, ShipmentServices shipmentServices, OrderRouteRepository orderRouteRepository, RouteRepository routeRepository, OrderService orderService)
        {
            _riderRepository = riderRepository ?? throw new ArgumentNullException(nameof(riderRepository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _orders = context.Set<Order>();
            _orderRiders = context.Set<OrderRider>();
            _shipmentServices = shipmentServices;
            _orderRouteRepository = orderRouteRepository;
            _routeRepository = routeRepository;
            _orderService = orderService;
            _orderRepository = orderRepository;
            _shipmentRepository = shipmentRepository;
        }

        public Rider RegisterRiderAsync(Rider rider)
        {
            if (rider == null)
                throw new ArgumentNullException(nameof(rider));

            if (rider.User == null || string.IsNullOrEmpty(rider.UserID))
                throw new InvalidOperationException("Rider must be associated with a user profile.");

            if (rider.Status == default)
                rider.Status = RiderStatusEnum.Available;

            _riderRepository.Add(rider);
            _riderRepository.CustomSaveChanges();

            return rider;
        }
        public async Task<RiderLocationDto> GetRiderLastLocationAsync(string riderId)
        {
            var location = await _context.Riders
                .Where(l => l.UserID == riderId)
                .OrderByDescending(l => l.Lastupdated)
                .Select(l => new RiderLocationDto
                {
                    RiderId = l.UserID,
                    Latitude = l.Lat,
                    Longitude = l.Lang,
                    LastUpdated = (DateTime)l.Lastupdated
                }  )
                .FirstOrDefaultAsync();

            return location;
        }

        public async Task<Rider> GetRiderProfileAsync(string riderId)
        {
            var rider = await _riderRepository.GetAsync(riderId);

            if (rider == null)
                throw new KeyNotFoundException($"Rider with ID {riderId} not found.");

            return rider;
        }


        // In RiderService.cs
        public async Task<string> GetRiderNameAsync(string riderId)
        {
            if (string.IsNullOrEmpty(riderId))
            {
                throw new ArgumentException("Rider ID cannot be null or empty");
            }

            var rider = await _riderRepository.GetAsync(riderId);

            if (rider == null)
            {
                throw new KeyNotFoundException($"Rider with ID {riderId} not found");
            }

            return rider.User?.Name ?? throw new InvalidOperationException("User name not found for rider");
        }

        //public async Task<List<Order>> GetAssignedOrdersAsync(string riderId)
        //{
        //    var rider = await _riderRepository.GetAsync(riderId);
        //    if (rider == null)
        //        throw new KeyNotFoundException($"Rider with ID {riderId} not found.");

        //    var assignedOrders = await _orderRiders
        //        .Where(or => or.RiderID == riderId && !or.IsDeleted)
        //        .Join(
        //            _orders.Where(o => !o.IsDeleted && o.State == OrderStateEnum.Pending),
        //            or => or.OrderID,
        //            o => o.Id,
        //            (or, o) => o)
        //        .Include(o => o.Customer)
        //        .Include(o => o.OrderRoute)
        //        .ToListAsync();

        //    return assignedOrders;
        //}
        //public async Task<string> GetBusinessOwnerByRiderIdAsync(string riderId)
        //{
        //    // Implement logic to find the business owner associated with the rider
        //    // This is a placeholder; adjust based on your database schema
        //    var rider = await _context.Riders
        //        .Where(r => r.UserID == riderId)
        //        .Select(r => r.BusinessID)
        //        .FirstOrDefaultAsync();

        //    return rider ?? string.Empty;
        //}

        public async Task<Order> AcceptOrderAsync(string riderId, int orderId)
        {
            var rider = await _riderRepository.GetAsync(riderId);
            if (rider == null)
                throw new KeyNotFoundException($"Rider with ID {riderId} not found.");

            if (rider.Status != RiderStatusEnum.Available)
                throw new InvalidOperationException($"Rider with ID {riderId} is not available to accept orders. Current status: {rider.Status}.");

            var orderRider = await _orderRiders
                .FirstOrDefaultAsync(or => or.OrderID == orderId && or.RiderID == riderId && !or.IsDeleted);
            if (orderRider == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found or not assigned to this rider.");

            var order = await _orders
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
            if (order == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");

            if (order.State != OrderStateEnum.Pending)
                throw new InvalidOperationException($"Order with ID {orderId} cannot be accepted. Current state: {order.State}.");

            //order.State = OrderStateEnum.Confirmed;
            //order.ModifiedBy = rider.UserID;
            //order.ModifiedAt = DateTime.UtcNow;

            await _orderService.UpdateOrderState(orderId, OrderStateEnum.Confirmed, riderId, rider.UserID);

            rider.Status = RiderStatusEnum.OnDelivery;


            await _context.SaveChangesAsync();
            return order;
        }
        public async Task<string> GetBusinessOwnerByRiderIdAsync(string riderId)
        {
            try
            {
                if (string.IsNullOrEmpty(riderId))
                {
                    return null;
                }

                // Assuming you have a DbContext and a table that maps Riders to BusinessOwners
                var rider = _riderRepository.GetBusinessOwnerByRiderId(riderId);

                if (rider == null)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(rider.BusinessOwner.UserID))
                {
                    return null;
                }

                return rider.BusinessOwner.UserID;
            }
            catch (Exception ex)
            {
                throw; // Rethrow the exception to be handled by the caller (e.g., GetShipment)
            }
        }
        public async Task UpdateRiderLocationAsync(RiderLocationDto location)
        {
            _riderLocations[location.RiderId] = location;
            await Task.CompletedTask;
        }

        public async Task<Order> RejectOrderAsync(string riderId, int orderId)
        {
            var rider = await _riderRepository.GetAsync(riderId);
            if (rider == null)
                throw new KeyNotFoundException($"Rider with ID {riderId} not found.");

            var orderRider = await _orderRiders
                .FirstOrDefaultAsync(or => or.OrderID == orderId && or.RiderID == riderId && !or.IsDeleted);
            if (orderRider == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found or not assigned to this rider.");

            var order = await _orders
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
            if (order == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");

            if (order.State != OrderStateEnum.Pending)
                throw new InvalidOperationException($"Order with ID {orderId} cannot be rejected. Current state: {order.State}.");

            order.State = OrderStateEnum.Cancelled;
            order.ModifiedBy = rider.UserID;
            order.ModifiedAt = DateTime.UtcNow;

            orderRider.IsDeleted = true;
            orderRider.ModifiedBy = rider.UserID;
            orderRider.ModifiedAt = DateTime.UtcNow;

            rider.Status = RiderStatusEnum.Available;

            order.RiderID = "";

            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order> UpdateDeliveryStatusAsync(string riderId, int orderId, OrderStateEnum newState)
        {
            

            var order =  ValidateOrderAsync(riderId, orderId, newState);
            await UpdateOrderStatusAsync(order, newState);
            await HandleShipmentAndRiderUpdatesAsync(orderId, riderId);

            await _context.SaveChangesAsync();

            return order;
        }

        private Order ValidateOrderAsync(string riderId, int orderId, OrderStateEnum newState)
        {
            var order =  _orderRepository.GetOrderById(orderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order with ID {orderId} not found.");
            }

            if (order.RiderID != riderId)
            {
                
                throw new InvalidOperationException("Rider is not assigned to this order.");
            }

            if (newState != OrderStateEnum.Delivered)
            {
                
                throw new InvalidOperationException("Only 'Delivered' status updates are supported.");
            }

            return order;
        }

        private async Task UpdateOrderStatusAsync(Order order, OrderStateEnum newState)
        {
            order.State = newState;
            order.ModifiedAt = DateTime.UtcNow;
        }

        private async Task HandleShipmentAndRiderUpdatesAsync(int orderId, string riderId)
        {
            var orderRoute = await _orderRepository.GetOrderRouteByOrderIdAsync(orderId);
            if (orderRoute?.Route?.Shipment == null || orderRoute.Route.Shipment.IsDeleted)
            {
                return;
            }

            var shipmentId = orderRoute.Route.ShipmentID;
            var shipmentOrders = await _orderRepository.GetOrdersByShipmentIdAsync((int)shipmentId);
            if (IsShipmentComplete(shipmentOrders, orderId))
            {
                await UpdateShipmentStatusAsync((int)shipmentId);
                await UpdateRiderStatusAsync(riderId);
            }
        }

        private bool IsShipmentComplete(List<Order> shipmentOrders, int currentOrderId)
        {
            return shipmentOrders.All(o => o.State == OrderStateEnum.Delivered || o.Id == currentOrderId);
        }

        private async Task UpdateShipmentStatusAsync(int shipmentId)
        {
            var shipment = await _shipmentRepository.GetShipmentByIdAsync(shipmentId);
            if (shipment != null)
            {
                shipment.ShipmentState = ShipmentStateEnum.Delivered; // Fixed from Delivered
                shipment.RealEndTime = DateTime.Now;
                shipment.ModifiedAt = DateTime.Now;
            }
        }

        private async Task UpdateRiderStatusAsync(string riderId)
        {
            var rider = await _riderRepository.GetRiderByIdAsync(riderId);
            if (rider != null)
            {
                rider.Status = RiderStatusEnum.Available;
            }
        }
        public async Task<List<Order>> StartDeliveriesAsync(string riderId, List<int> orderIds)
        {
            if (_riderRepository == null || _orderRepository == null || _orderRiders == null)
                throw new NullReferenceException("One or more repositories are not initialized in RiderService.");

            if (orderIds == null || orderIds.Count == 0)
                throw new ArgumentException("No order IDs provided");

            var rider = await _riderRepository.GetAsync(riderId);
            if (rider == null)
                throw new KeyNotFoundException($"Rider with ID {riderId} not found.");

            if (rider.Status != RiderStatusEnum.Available)
                throw new InvalidOperationException($"Rider with ID {riderId} cannot start delivery. Current status: {rider.Status}.");

            var successfulOrders = new List<Order>();

            foreach (var orderId in orderIds.Distinct())
            {
                try
                {
                    var orderRider = await _orders
                        .FirstOrDefaultAsync(or => or.Id == orderId && or.RiderID == riderId && !or.IsDeleted);
                    if (orderRider == null)
                        throw new KeyNotFoundException($"Order with ID {orderId} not found or not assigned to this rider.");

                    var order = await _orderRepository.GetAsync(orderId);
                    if (order == null)
                        throw new KeyNotFoundException($"Order with ID {orderId} not found.");

                    if (order.State != OrderStateEnum.Confirmed)
                        throw new InvalidOperationException($"Order with ID {orderId} cannot be marked as shipped. Current state: {order.State}.");

                    order.State = OrderStateEnum.Shipped;
                    order.ModifiedBy = rider.UserID;
                    order.ModifiedAt = DateTime.UtcNow;

                    successfulOrders.Add(order);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to process order {orderId}: {ex.Message}");
                }
            }

            if (successfulOrders.Count > 0)
            {

                var shipment = new Shipment
                {
                    RiderID = riderId,
                    Rider = rider,
                    startTime = DateTime.UtcNow,
                    ShipmentState = ShipmentStateEnum.InTransit,
                    ModifiedBy = rider.UserID,
                    ModifiedAt = DateTime.UtcNow,
                };

                _shipmentRepository.Add(shipment);

                rider.Status = RiderStatusEnum.OnDelivery;

                _orderRepository.CustomSaveChanges();
                _riderRepository.CustomSaveChanges();
                _shipmentRepository.CustomSaveChanges();
            }

            return successfulOrders;
        }
        private bool IsValidStateTransition(OrderStateEnum currentState, OrderStateEnum newState)
        {
            return (currentState, newState) switch
            {
                (OrderStateEnum.Confirmed, OrderStateEnum.Shipped) => true,
                (OrderStateEnum.Shipped, OrderStateEnum.Delivered) => true,
                (OrderStateEnum.Confirmed, OrderStateEnum.Cancelled) => true,
                (OrderStateEnum.Shipped, OrderStateEnum.Cancelled) => true,
                _ => false
            };
        }
    }
}