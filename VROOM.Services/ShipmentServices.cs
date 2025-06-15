using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.ViewModels;
using Microsoft.EntityFrameworkCore;
using ViewModels.Shipment;
using VROOM.Models.Dtos;
using VROOM.Data;
using Microsoft.Extensions.Logging;
using VROOM.Repository;
using ViewModels.Route;

namespace VROOM.Services
{
    public class ShipmentServices
    {
        private readonly ShipmentRepository shipmentRepository;
        private readonly RouteRepository routeRepository;
        private readonly ILogger<ShipmentServices> logger;
        private readonly RiderRepository riderRepository;
        private readonly OrderRepository orderRepository;
        private readonly NotificationService notificationService;
        private readonly VroomDbContext dbContext;

        public ShipmentServices(
            ShipmentRepository _shipmentRepository,
            RouteRepository _routeRepository,
            RiderRepository _riderRepository,
            OrderRepository _orderRepository,
            NotificationService _notificationService,
            VroomDbContext _dbContext,
            ILogger<ShipmentServices> _logger)
        {
            shipmentRepository = _shipmentRepository ?? throw new ArgumentNullException(nameof(_shipmentRepository));
            routeRepository = _routeRepository ?? throw new ArgumentNullException(nameof(_routeRepository));
            riderRepository = _riderRepository ?? throw new ArgumentNullException(nameof(_riderRepository));
            orderRepository = _orderRepository ?? throw new ArgumentNullException(nameof(_orderRepository));
            notificationService = _notificationService ?? throw new ArgumentNullException(nameof(_notificationService));
            dbContext = _dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
            logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
        }

        public async Task<List<ShipmentDto>> GetAllShipmentsAsync()
        {
            var shipments = await shipmentRepository.GetList(s => !s.IsDeleted)
                .Include(s => s.Routes)
                .ThenInclude(r => r.OrderRoutes)
                .Include(s => s.waypoints)
                .ToListAsync();

            return shipments.Select(s => new ShipmentDto
            {
                Id = s.Id,
                StartTime = s.startTime,
                RiderID = s.RiderID,
                BeginningLat = s.BeginningLat,
                BeginningLang = s.BeginningLang,
                BeginningArea = s.BeginningArea,
                EndLat = s.EndLat,
                EndLang = s.EndLang,
                EndArea = s.EndArea,
                Zone = s.zone,
                MaxConsecutiveDeliveries = s.MaxConsecutiveDeliveries,
                InTransiteBeginTime = s.InTransiteBeginTime,
                RealEndTime = s.RealEndTime,
                ShipmentState = s.ShipmentState,
                Waypoints = s.waypoints?.Select(w => new WaypointDto
                {
                    Latitude = w.Lat,
                    Longitude = w.Lang,
                    Area = w.Area
                }).ToList(),
                Routes = s.Routes.Select(r => new TheRouteDto
                {
                    Id = r.Id,
                    OriginLat = r.OriginLat,
                    OriginLang = r.OriginLang,
                    OriginArea = r.OriginArea,
                    DestinationLat = r.DestinationLat,
                    DestinationLang = r.DestinationLang,
                    DestinationArea = r.DestinationArea,
                    Start = r.Start,
                    DateTime = r.dateTime,
                    SafetyIndex = r.SafetyIndex,
                    ShipmentID = r.ShipmentID,
                    OrderIds = r.OrderRoutes.Select(or => or.OrderID).ToList()
                }).ToList()
            }).ToList();
        }

        public async Task<bool> RiderExistsAsync(string? riderId)
        {
            if (string.IsNullOrEmpty(riderId))
                return true; // Allow null/empty
            return await dbContext.Riders.AnyAsync(r => r.UserID == riderId); // Fixed typo: r.I to r.Id
        }

        public async Task<ShipmentDto> GetShipmentByIdAsync(int shipmentId)
        {
            var shipment = await shipmentRepository.GetList(s => s.Id == shipmentId && !s.IsDeleted)
                .Include(s => s.Routes)
                .ThenInclude(r => r.OrderRoutes)
                .Include(s => s.waypoints)
                .FirstOrDefaultAsync();

            if (shipment == null)
                return null;

            return new ShipmentDto
            {
                Id = shipment.Id,
                StartTime = shipment.startTime,
                RiderID = shipment.RiderID,
                BeginningLat = shipment.BeginningLat,
                BeginningLang = shipment.BeginningLang,
                BeginningArea = shipment.BeginningArea,
                EndLat = shipment.EndLat,
                EndLang = shipment.EndLang,
                EndArea = shipment.EndArea,
                Zone = shipment.zone,
                MaxConsecutiveDeliveries = shipment.MaxConsecutiveDeliveries,
                InTransiteBeginTime = shipment.InTransiteBeginTime,
                RealEndTime = shipment.RealEndTime,
                ShipmentState = shipment.ShipmentState,
                Waypoints = shipment.waypoints?.Select(w => new WaypointDto
                {
                    Latitude = w.Lat,
                    Longitude = w.Lang,
                    Area = w.Area
                }).ToList(),
                Routes = shipment.Routes?.Select(r => new TheRouteDto
                {
                    Id = r.Id,
                    OriginLat = r.OriginLat,
                    OriginLang = r.OriginLang,
                    OriginArea = r.OriginArea,
                    DestinationLat = r.DestinationLat,
                    DestinationLang = r.DestinationLang,
                    DestinationArea = r.DestinationArea,
                    Start = r.Start,
                    DateTime = r.dateTime,
                    SafetyIndex = r.SafetyIndex,
                    ShipmentID = r.ShipmentID,
                    OrderIds = r.OrderRoutes?.Select(or => or.OrderID).ToList()
                }).ToList()
            };
        }

        public async Task<Shipment> CreateShipment(AddShipmentVM addShipmentVM)
        {
            // Enforce RiderID as null during creation
            if (!string.IsNullOrEmpty(addShipmentVM.RiderID))
            {
                logger.LogWarning($"RiderID must be null during shipment creation. Received: {addShipmentVM.RiderID}");
                throw new ArgumentException("RiderID must be null when creating a shipment.");
            }

            // Validate orders
            if (addShipmentVM.OrderIds == null || !addShipmentVM.OrderIds.Any())
            {
                logger.LogWarning("No orders provided for shipment creation.");
                throw new ArgumentException("At least one order must be selected for the shipment.");
            }

            // Fetch orders with their OrderRoute and Route
            var orders = await orderRepository.GetList(o => addShipmentVM.OrderIds.Contains(o.Id) && !o.IsDeleted)
                .Include(o => o.OrderRoute)
                .ThenInclude(or => or.Route)
                .ToListAsync();

            if (orders.Count != addShipmentVM.OrderIds.Count)
            {
                logger.LogWarning("One or more selected orders are invalid or deleted.");
                throw new ArgumentException("One or more selected orders are invalid or deleted.");
            }

            // Ensure all orders have routes and are in a valid state
            foreach (var order in orders)
            {
                if (order.OrderRoute == null || order.OrderRoute.Route == null)
                {
                    logger.LogWarning($"Order {order.Id} does not have a valid route.");
                    throw new ArgumentException($"Order {order.Id} does not have a valid route.");
                }
                if (order.State != OrderStateEnum.Created && order.State != OrderStateEnum.Pending)
                {
                    logger.LogWarning($"Order {order.Id} is not in a valid state for shipment (Current: {order.State}).");
                    throw new ArgumentException($"Order {order.Id} is not in a valid state for shipment (must be Created or Pending).");
                }
            }

            // Determine shipment coordinates and area from first and last order routes
            var firstOrderRoute = orders.First().OrderRoute.Route;
            var lastOrderRoute = orders.Last().OrderRoute.Route;

            // Create waypoints from destination coordinates of each order's route
            var waypoints = orders.Select(o => new
            {
                Latitude = o.OrderRoute.Route.DestinationLat,
                Longitude = o.OrderRoute.Route.DestinationLang,
                Area = o.OrderRoute.Route.DestinationArea
            }).ToList();
            var waypointsJson = JsonSerializer.Serialize(waypoints);

            // Calculate total weight to determine MaxConsecutiveDeliveries
            var totalWeight = orders.Sum(o => o.Weight);
            var maxConsecutiveDeliveries = addShipmentVM.MaxConsecutiveDeliveries > 0
                ? addShipmentVM.MaxConsecutiveDeliveries
                : CalculateMaxConsecutiveDeliveries(totalWeight);

            // Create shipment
            var shipment = new Shipment
            {
                startTime = addShipmentVM.startTime,
                RiderID = null, // Explicitly set to null
                BeginningLat = firstOrderRoute.OriginLat,
                BeginningLang = firstOrderRoute.OriginLang,
                BeginningArea = firstOrderRoute.OriginArea,
                EndLat = lastOrderRoute.DestinationLat,
                EndLang = lastOrderRoute.DestinationLang,
                EndArea = lastOrderRoute.DestinationArea,
                zone = orders.First().zone, // Assume all orders share the same zone
                MaxConsecutiveDeliveries = maxConsecutiveDeliveries,
                InTransiteBeginTime = addShipmentVM.InTransiteBeginTime ?? CalculateInTransiteBeginTime(orders),
                RealEndTime = addShipmentVM.EndTime,
                ShipmentState = ShipmentStateEnum.Created,
                waypoints = waypoints.Select(w => new Waypoint
                {
                    Lat = w.Latitude,
                    Lang = w.Longitude,
                    Area = w.Area
                }).ToList()
            };

            // Create route for shipment
            var newRoute = new Route
            {
                OriginLat = shipment.BeginningLat,
                OriginLang = shipment.BeginningLang,
                OriginArea = shipment.BeginningArea,
                DestinationLat = shipment.EndLat,
                DestinationLang = shipment.EndLang,
                DestinationArea = shipment.EndArea,
                Waypoints = waypointsJson,
                Start = shipment.startTime,
                dateTime = DateTime.Now,
                SafetyIndex = 0 // Default value, adjust as needed
            };

            // Save route and shipment
            routeRepository.Add(newRoute);
            routeRepository.CustomSaveChanges();

            shipmentRepository.Add(shipment);
            shipmentRepository.CustomSaveChanges();

            // Link shipment to route
            newRoute.ShipmentID = shipment.Id;
            routeRepository.Update(newRoute);
            routeRepository.CustomSaveChanges();

            // Update orders to reference the shipment and set state
            foreach (var order in orders)
            {
                order.State = OrderStateEnum.Created;
                order.OrderRoute.Route.ShipmentID = shipment.Id; // Link order's route to shipment
                orderRepository.Update(order);
                routeRepository.Update(order.OrderRoute.Route);
            }

            routeRepository.CustomSaveChanges();
            orderRepository.CustomSaveChanges();

            return shipment;
        }

        public async Task<Shipment> UpdateShipmentState(int shipmentId, ShipmentStateEnum shipmentState, string? riderId, string businessOwnerId)
        {
            // Validate rider if provided
            if (!string.IsNullOrEmpty(riderId))
            {
                var rider = await riderRepository.GetAsync(riderId);
                if (rider == null)
                {
                    logger.LogWarning($"Rider with ID {riderId} not found for shipment {shipmentId}.");
                    throw new ArgumentException($"Rider with ID {riderId} not found.");
                }
            }

            // Fetch shipment
            var shipment = await shipmentRepository.GetAsync(shipmentId);
            if (shipment == null || shipment.IsDeleted)
            {
                logger.LogWarning($"Shipment with ID {shipmentId} not found or deleted.");
                return null;
            }

            // Update shipment properties
            shipment.RiderID = riderId; // Allow null or valid rider ID
            shipment.ShipmentState = shipmentState;
            shipment.ModifiedBy = businessOwnerId;
            shipment.ModifiedAt = DateTime.Now;

            // Additional logic for specific states
            if (shipmentState == ShipmentStateEnum.Created)
            {
                shipment.InTransiteBeginTime = DateTime.Now; // Start transit on acceptance
            }
            else if (shipmentState == ShipmentStateEnum.Cancelled)
            {
                shipment.RealEndTime = DateTime.Now; // Mark end time for rejected shipment
                // Unlink from route and update orders
                var route = await routeRepository.GetList(r => r.ShipmentID == shipmentId).FirstOrDefaultAsync();
                if (route != null)
                {
                    route.ShipmentID = null;
                    routeRepository.Update(route);
                    routeRepository.CustomSaveChanges();
                }
                var orders = await orderRepository.GetList(o => o.OrderRoute != null && o.OrderRoute.Route != null && o.OrderRoute.Route.ShipmentID == shipmentId)
                    .Include(o => o.OrderRoute)
                    .ThenInclude(or => or.Route)
                    .ToListAsync();
                foreach (var order in orders)
                {
                    order.State = OrderStateEnum.Cancelled;
                    order.OrderRoute.Route.ShipmentID = null;
                    orderRepository.Update(order);
                    routeRepository.Update(order.OrderRoute.Route);
                }
                orderRepository.CustomSaveChanges();
                routeRepository.CustomSaveChanges();
            }

            // Save changes
            shipmentRepository.Update(shipment);
            shipmentRepository.CustomSaveChanges();

            // Send notification
            string message = shipmentState == ShipmentStateEnum.Assigned
                ? $"Shipment {shipmentId} has been accepted."
                : $"Shipment {shipmentId} has been rejected.";
            //await notificationService.SendShipmentStatusUpdateAsync(shipment.RiderID, message, shipmentId, shipmentState.ToString());

            return shipment;
        }

        public async Task DeleteShipmentAsync(int shipmentId)
        {
            var shipment = await shipmentRepository.GetAsync(shipmentId);
            if (shipment != null && !shipment.IsDeleted)
            {
                shipment.IsDeleted = true;
                shipment.ModifiedAt = DateTime.Now;
                shipmentRepository.Update(shipment);
                shipmentRepository.CustomSaveChanges();

                // Unlink from route and update orders
                var route = await routeRepository.GetList(r => r.ShipmentID == shipmentId).FirstOrDefaultAsync();
                if (route != null)
                {
                    route.ShipmentID = null;
                    routeRepository.Update(route);
                    routeRepository.CustomSaveChanges();
                }

                var orders = await orderRepository.GetList(o => o.OrderRoute != null && o.OrderRoute.Route != null && o.OrderRoute.Route.ShipmentID == shipmentId)
                    .Include(o => o.OrderRoute)
                    .ThenInclude(or => or.Route)
                    .ToListAsync();
                foreach (var order in orders)
                {
                    order.State = OrderStateEnum.Cancelled;
                    order.OrderRoute.Route.ShipmentID = null;
                    orderRepository.Update(order);
                    routeRepository.Update(order.OrderRoute.Route);
                }
                orderRepository.CustomSaveChanges();
                routeRepository.CustomSaveChanges();
            }
        }

        private int CalculateMaxConsecutiveDeliveries(float totalWeight)
        {
            // Example logic: Adjust max deliveries based on weight
            if (totalWeight <= 50) return 10;
            if (totalWeight <= 100) return 7;
            if (totalWeight <= 200) return 5;
            return 3;
        }

        private DateTime CalculateInTransiteBeginTime(List<Order> orders)
        {
            // Determine InTransiteBeginTime based on highest priority order
            var highestPriority = orders
                .Select(o => o.OrderPriority)
                .Min(); // Lower enum value means higher priority (e.g., HighUrgent = 0)
            var prepareTime = orders.Max(o => o.PrepareTime ?? TimeSpan.Zero);

            switch (highestPriority)
            {
                case OrderPriorityEnum.HighUrgent:
                    return DateTime.Now.Add(prepareTime);
                case OrderPriorityEnum.Urgent:
                    return DateTime.Now.Add(prepareTime + TimeSpan.FromMinutes(5));
                default:
                    return DateTime.Now.Add(prepareTime + TimeSpan.FromMinutes(10));
            }
        }
    }
}