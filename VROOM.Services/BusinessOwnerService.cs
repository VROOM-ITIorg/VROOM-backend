using Hangfire.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NuGet.Protocol.Core.Types;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using ViewModels.Shipment;
using ViewModels.User;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Repositories;
using VROOM.Repository;
using VROOM.ViewModels;
namespace VROOM.Services
{
    public class ClientHub : Hub
    {
        public async Task SendShipmentRequest(string riderId, object message)
        {
            await Clients.User(riderId).SendAsync("ReceiveShipmentRequest", message);
        }
    }
    public class BusinessOwnerService
    {
        //private readonly MyDbContext businessOwnerRepo;
        private readonly ILogger<BusinessOwnerService> _logger;
        private readonly Microsoft.AspNetCore.Identity.UserManager<User> userManager;
        private readonly BusinessOwnerRepository businessOwnerRepo;
        private readonly UserService userService;
        private readonly OrderRepository orderRepository;
        private readonly RiderRepository riderRepository;
        private readonly RouteRepository routeRepository;
        private readonly ShipmentRepository shipmentRepository;
        private readonly ShipmentServices shipmentServices;
        private readonly OrderRiderRepository orderRiderRepository;
        private readonly OrderRouteRepository orderRouteRepository;
        private readonly Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> _roleManager;
        private readonly UserService _userService;
        private readonly OrderService orderService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserRepository _userRepository;
        private readonly IHubContext<ClientHub> _hubContext;
        private readonly ConcurrentDictionary<string, ShipmentConfirmation> _confirmationStore;

        public BusinessOwnerService(
            Microsoft.AspNetCore.Identity.UserManager<User> _userManager,
            BusinessOwnerRepository _businessOwnerRepo,
            OrderRepository _orderRepository,
            RiderRepository _riderRepository
            ,
            RoleManager<IdentityRole> roleManager
            ,
            UserService userService,
            UserRepository userRepository,

            OrderRiderRepository orderRiderRepository,
            ILogger<BusinessOwnerService> logger,
            IHttpContextAccessor httpContextAccessor,
            OrderRouteRepository _orderRouteRepository,
            OrderService _orderService,
            RouteRepository _routeRepository,
            ShipmentServices _shipmentServices,
            ShipmentRepository _shipmentRepository,
             IHubContext<ClientHub> hubContext,
    ConcurrentDictionary<string, ShipmentConfirmation> confirmationStore
            )
        {
            userManager = _userManager;
            businessOwnerRepo = _businessOwnerRepo;
            orderRepository = _orderRepository;
            riderRepository = _riderRepository;
            orderRouteRepository = _orderRouteRepository;
            orderService = _orderService;
            routeRepository = _routeRepository;
            _roleManager = roleManager;
            this.orderRiderRepository = orderRiderRepository;
            _userService = userService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
            shipmentServices = _shipmentServices;
            shipmentRepository = _shipmentRepository;
            _hubContext = hubContext;
            _confirmationStore = confirmationStore;
        }


        public bool UpdateBusinessRegistration(BusinessOwner updatedBusinessInfo)
        {
            var existingBusiness = businessOwnerRepo.GetAsync(updatedBusinessInfo.UserID).Result;
            if (existingBusiness == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(updatedBusinessInfo.BankAccount))
            {
                existingBusiness.BankAccount = updatedBusinessInfo.BankAccount;

            }
            if (!string.IsNullOrEmpty(existingBusiness.BusinessType))
            {
                existingBusiness.BusinessType = updatedBusinessInfo.BusinessType;
            }


            businessOwnerRepo.CustomSaveChanges();
            return true;
        }


        public BusinessOwnerViewModel GetBusinessDetails(string businessOwnerId)
        {
            var businessOwner = businessOwnerRepo.GetAsync(businessOwnerId).Result;

            return new BusinessOwnerViewModel
            {
                UserID = businessOwner.UserID,
                BankAccount = businessOwner.BankAccount,
                BusinessType = businessOwner.BusinessType
            };
        }



        public async Task<Result<RiderVM>> CreateRiderAsync(RiderRegisterRequest request)
        {
            _logger.LogInformation("Creating rider with email: {Email}", request.Email);


            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {

                var existingUser = await userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("A user with this email already exists: {Email}", request.Email);
                    return Result<RiderVM>.Failure("A user with this email already exists.");
                }

                var user = new User
                {
                    UserName = request.Email,
                    Email = request.Email,
                    Name = request.Name,
                    ProfilePicture = request.ProfilePicture
                };

                _logger.LogInformation("Attempting to create user: {Email}", request.Email);
                var creationResult = await userManager.CreateAsync(user, request.Password);
                if (!creationResult.Succeeded)
                {
                    var errorMessages = string.Join(",", creationResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create user: {Email}. Errors: {Errors}", request.Email, errorMessages);
                    return Result<RiderVM>.Failure(errorMessages);
                }

                _logger.LogInformation("User created successfully: {Email}", request.Email);


                var role = RoleConstants.Rider;
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    _logger.LogInformation("Role {Role} does not exist. Creating role...", role);
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }


                _logger.LogInformation("Assigning role {Role} to user: {Email}", role, request.Email);
                await userManager.AddToRoleAsync(user, role);

                var rider = new Rider
                {
                    UserID = user.Id,
                    BusinessID = request.BusinessID,
                    Status = RiderStatusEnum.Available,
                    VehicleType = request.VehicleType,
                    VehicleStatus = request.VehicleStatus,
                    ExperienceLevel = request.ExperienceLevel,
                    Lat = request.Location.Lat,
                    Lang = request.Location.Lang,
                    Area = request.Location.Area,
                    Rating = 0
                };

                _logger.LogInformation("Adding rider to the repository for user: {Email}", request.Email);
                riderRepository.Add(rider);
                riderRepository.CustomSaveChanges();


                var result = new RiderVM
                {
                    UserID = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    BusinessID = rider.BusinessID,
                    VehicleType = rider.VehicleType,
                    VehicleStatus = rider.VehicleStatus,
                    ExperienceLevel = rider.ExperienceLevel,
                    Location = new LocationDto
                    {
                        Lat = rider.Lat,
                        Lang = rider.Lang,
                        Area = rider.Area
                    },
                    Status = rider.Status
                };

                scope.Complete();

                _logger.LogInformation("Rider created successfully: {Email}", request.Email);

                return Result<RiderVM>.Success(result);
            }
        }


        //will be refactored

        public async Task<Result<string>> ChangeRiderPasswordAsync(string riderId, string newPassword)
        {

            var updateResult = await _userService.UpdatePasswordAsync(riderId, newPassword);

            if (!updateResult.IsSuccess)
            {
                return Result<string>.Failure(updateResult.Error);
            }


            return Result<string>.Success("Password updated successfully");
        }



        public class BusinessOwnerRegisterRequest
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string ProfilePicture { get; set; }

            public string BankAccount { get; set; }
            public string BusinessType { get; set; }
        }



        public async Task<Result<BusinessOwnerViewModel>> CreateBusinessOwnerAsync(BusinessOwnerRegisterRequest request)
        {
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Result<BusinessOwnerViewModel>.Failure("A user with this email already exists.");
            }

            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                Name = request.Name,
                ProfilePicture = request.ProfilePicture
            };

            var creationResult = await userManager.CreateAsync(user, request.Password);
            if (!creationResult.Succeeded)
            {
                return Result<BusinessOwnerViewModel>.Failure(string.Join(",", creationResult.Errors.Select(e => e.Description)));
            }


            var role = RoleConstants.BusinessOwner;
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            await userManager.AddToRoleAsync(user, role);


            var businessOwner = new BusinessOwner
            {
                UserID = user.Id,
                User = user,
                BankAccount = request.BankAccount,
                BusinessType = request.BusinessType
            };

            businessOwnerRepo.Add(businessOwner);
            businessOwnerRepo.CustomSaveChanges();

            var businessowner = new BusinessOwnerViewModel
            {
                UserID = user.Id,
                Name = user.Name,
                Email = user.Email,
                //ProfilePicture = user.ProfilePicture,
                BankAccount = request.BankAccount,
                BusinessType = request.BusinessType
            };

            return Result<BusinessOwnerViewModel>.Success(businessowner);
        }




        public async Task<bool> AssignShipmentToRiderAsync(int orderId, string riderId)
        {
            try
            {
                var businessOwnerId = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("Assign failed: BusinessOwner ID not found in context.");
                    return false;
                }

                var businessOwner = await businessOwnerRepo.GetAsync(businessOwnerId);
                if (businessOwner == null)
                {
                    _logger.LogWarning($"Assign failed: No BusinessOwner found with ID {businessOwnerId}");
                    return false;
                }

                var order = await orderRepository.GetAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    _logger.LogWarning($"Assign failed: Order ID {orderId} not found or deleted.");
                    return false;
                }

                if (order.State != OrderStateEnum.Created && order.State != OrderStateEnum.Pending)
                {
                    _logger.LogWarning($"Assign failed: Order ID {orderId} is in invalid state: {order.State}");
                    return false;
                }

                var rider = await riderRepository.GetAsync(riderId);
                if (rider == null || rider.BusinessID != businessOwner.UserID || rider.Status != RiderStatusEnum.Available)
                {
                    _logger.LogWarning($"Assign failed: Rider ID {riderId} is invalid or unavailable.");
                    return false;
                }


                await orderService.UpdateOrderState(order.Id, OrderStateEnum.Pending, riderId, businessOwnerId);
                // داخل الدالة AssignShipmentToRiderAsync
                await NotifyRiderForShipmentConfirmation(riderId, order.Id, businessOwnerId);
                //StartShipmentConfirmationTimer(riderId, order.Id, businessOwnerId);

                //orderRepository.Update(order);
                //await Task.Run(() => orderRepository.CustomSaveChanges());


                // Create Shipment
                var orderRoute = await orderRouteRepository.GetOrderRouteByOrderID(orderId);

                var route = await routeRepository.GetAsync(orderRoute.RouteID);
                // shipment WE WILL REUSE IN GETRIDER

                var shipment = await shipmentRepository
                    .GetList(sh => !sh.IsDeleted && (sh.Routes == null || sh.Routes.Count < sh.MaxConsecutiveDeliveries) && (sh.ShipmentState == ShipmentStateEnum.Created || sh.ShipmentState == ShipmentStateEnum.Assigned) && sh.zone == order.zone)
                    .Include(s => s.Routes)
                    .FirstOrDefaultAsync();

                if (shipment != null)
                {
                    // Update Route => Add Shipment Id
                    route.ShipmentID = shipment.Id;
                    routeRepository.Update(route);
                    routeRepository.CustomSaveChanges();

                    // تعديل نهاية الشحنة لو المسار الجديد بعد المسار القديم
                    var lastRoute = shipment.Routes?.OrderByDescending(r => r.DestinationLang).ThenByDescending(r => r.DestinationLat).FirstOrDefault();

                    if (lastRoute != null)
                    {
                        // هنحسب المسافة التقريبية ما بين نقطة النهاية بتاعة آخر Route والنقطة الجديدة
                        double lastLat = lastRoute.DestinationLat;
                        double lastLng = lastRoute.DestinationLang;

                        double newLat = route.DestinationLat;
                        double newLng = route.DestinationLang;

                        // Calculate Distance Between Two Points
                        double distance = Math.Sqrt(Math.Pow(newLat - lastLat, 2) + Math.Pow(newLng - lastLng, 2));

                        // نحدد Threshold صغير نقول لو أقل من كده يبقى هو في نفس الاتجاه أو قريب
                        double threshold = 0.01;

                        if (distance > threshold)
                        {
                            Waypoint waypoint = new Waypoint() { ShipmentID = shipment.Id, Lang = shipment.EndLang, Lat = shipment.EndLat, Area = shipment.EndArea };

                            shipment.waypoints.Add(waypoint);
                            // المسافة بعيدة – يبقى ممكن نحدث الـ shipment ونخلي EndLocation بتاعه هو ده
                            shipment.EndLat = newLat;
                            shipment.EndLang = newLng;
                            shipment.EndArea = route.DestinationArea;
                        }
                        else
                        {
                            Waypoint waypoint = new Waypoint() { ShipmentID = shipment.Id, Lang = route.DestinationLang, Lat = route.DestinationLat, Area = route.DestinationArea };
                            shipment.waypoints.Add(waypoint);
                        }

                        // بترجع تعمل Update للـ shipment بعد التعديل
                        shipmentRepository.Update(shipment);
                        shipmentRepository.CustomSaveChanges();
                    }
                }
                else
                {
                    // Create New Shipment With its Details 
                    await shipmentServices.CreateShipment(new AddShipmentVM
                    {
                        startTime = route.Start,
                        RiderID = riderId,
                        BeginningLang = route.OriginLang,
                        BeginningLat = route.OriginLat,
                        BeginningArea = route.OriginArea,
                        EndLang = route.DestinationLang,
                        EndLat = route.DestinationLat,
                        EndArea = route.DestinationArea,
                        zone = order.zone,
                        MaxConsecutiveDeliveries = 10
                    }, route);

                }



                _logger.LogInformation($"Order {orderId} successfully assigned to Rider {riderId} by BusinessOwner {businessOwnerId}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while assigning Order {orderId} to Rider {riderId}.");
                return false;
            }
        }


        public async Task<OrderDetailsViewModel?> ViewAssignedOrderAsync(int orderId)
        {
            try
            {
                var riderId = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(riderId))
                {
                    _logger.LogWarning("View failed: Rider ID not found in context.");
                    return null;
                }

                var rider = await riderRepository.GetAsync(riderId);
                if (rider == null || rider.Status != RiderStatusEnum.Available)
                {
                    _logger.LogWarning($"View failed: Rider ID {riderId} not found or not available.");
                    return null;
                }

                var order = await orderRepository.GetAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    _logger.LogWarning($"View failed: Order ID {orderId} not found or deleted.");
                    return null;
                }

                if (order.RiderID != riderId)
                {
                    _logger.LogWarning($"View failed: Rider {riderId} attempted to view unassigned Order {orderId}.");
                    return null;
                }

                _logger.LogInformation($"Rider {riderId} successfully viewed Order {orderId}.");

                return new OrderDetailsViewModel
                {
                    Id = order.Id,
                    Title = order.Title,
                    State = order.State,
                    RiderName = order.Rider.User.Name,
                    CustomerName = order.Customer.User.Name,
                    BusinessOwner = order.Rider.BusinessOwner.User.Name,
                    Priority = order.OrderPriority,
                    OrderPrice = order.OrderPrice,
                    DeliveryPrice = order.DeliveryPrice,
                    Date = order.Date
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while Rider was viewing Order {orderId}.");
                return null;
            }
        }

        public async Task<Result<List<RiderVM>>> GetRiders()
        {
            try
            {
                // Extract Business Owner ID from the HTTP context
                var businessOwnerId = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("Failed to retrieve riders: Business Owner ID not found in token.");
                    return Result<List<RiderVM>>.Failure("Business Owner ID not found in token.");
                }

                // Verify Business Owner exists
                var businessOwner = await businessOwnerRepo.GetAsync(businessOwnerId);
                if (businessOwner == null)
                {
                    _logger.LogWarning("Failed to retrieve riders: Business Owner with ID {BusinessOwnerId} not found.", businessOwnerId);
                    return Result<List<RiderVM>>.Failure("Business Owner not found.");
                }

                // Verify the caller is a Business Owner
                var roles = await userManager.GetRolesAsync(businessOwner.User);
                if (!roles.Contains(RoleConstants.BusinessOwner))
                {
                    _logger.LogWarning("Failed to retrieve riders: Caller with ID {BusinessOwnerId} is not a Business Owner.", businessOwnerId);
                    return Result<List<RiderVM>>.Failure("Caller is not a Business Owner.");
                }

                // Fetch riders associated with the Business Owner
                var riders = await riderRepository.GetRidersForBusinessOwnerAsync(businessOwnerId);

                // Map riders to RiderVM
                var riderVMs = riders.Select(r => new RiderVM
                {
                    UserID = r.UserID,
                    Name = r.User.Name,
                    Email = r.User.Email,
                    BusinessID = r.BusinessID,
                    VehicleType = r.VehicleType,
                    VehicleStatus = r.VehicleStatus,
                    ExperienceLevel = r.ExperienceLevel,
                    Location = new LocationDto
                    {
                        Lat = r.Lat,
                        Lang = r.Lang,
                        Area = r.Area
                    },
                    Status = r.Status
                }).ToList();

                _logger.LogInformation("Successfully retrieved {RiderCount} riders for Business Owner with ID {BusinessOwnerId}.", riderVMs.Count, businessOwnerId);
                return Result<List<RiderVM>>.Success(riderVMs);
            }
            catch (Exception ex)
            {
                //  _logger.LogError(ex, "An error occurred while retrieving riders for Business Owner with ID {BusinessOwnerId}.", businessOwnerId);
                return Result<List<RiderVM>>.Failure("An error occurred while retrieving riders.");
            }
        }

        public async Task<Result<List<CustomerVM>>> GetCustomers()
        {
            try
            {
                // Extract Business Owner ID from the HTTP context
                var businessOwnerId = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("Failed to retrieve customers: Business Owner ID not found in token.");
                    return Result<List<CustomerVM>>.Failure("Business Owner ID not found in token.");
                }

                // Verify Business Owner exists
                var businessOwner = await businessOwnerRepo.GetAsync(businessOwnerId);
                if (businessOwner == null)
                {
                    _logger.LogWarning("Failed to retrieve customers: Business Owner with ID {BusinessOwnerId} not found.", businessOwnerId);
                    return Result<List<CustomerVM>>.Failure("Business Owner not found.");
                }

                // Verify the caller is a Business Owner
                var roles = await userManager.GetRolesAsync(businessOwner.User);
                if (!roles.Contains(RoleConstants.BusinessOwner))
                {
                    _logger.LogWarning("Failed to retrieve customers: Caller with ID {BusinessOwnerId} is not a Business Owner.", businessOwnerId);
                    return Result<List<CustomerVM>>.Failure("Caller is not a Business Owner.");
                }

                // Fetch customers who have placed orders managed by the Business Owner's riders
                var customers = await _userRepository.GetCustomersByBusinessOwnerIdAsync(businessOwnerId);

                // Map customers to CustomerVM
                var customerVMs = new List<CustomerVM>();
                foreach (var customer in customers)
                {
                    var customerRoles = await userManager.GetRolesAsync(customer);
                    if (customerRoles.Contains(RoleConstants.Customer))
                    {
                        customerVMs.Add(new CustomerVM
                        {
                            UserID = customer.Id,
                            Name = customer.Name,
                            Email = customer.Email,
                            Location = customer.Address != null ? new LocationDto
                            {
                                Lat = customer.Address.Lat,
                                Lang = customer.Address.Lang,
                                Area = customer.Address.Area
                            } : null
                        });
                    }
                }

                _logger.LogInformation("Successfully retrieved {CustomerCount} customers for Business Owner with ID {BusinessOwnerId}.", customerVMs.Count, businessOwnerId);
                return Result<List<CustomerVM>>.Success(customerVMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving customers for Business Owner with ID ");
                return Result<List<CustomerVM>>.Failure("An error occurred while retrieving customers.");
            }
        }

         
        public async Task<bool> PrepareOrder(OrderCreateViewModel _orderCreateVM)
        {
            try
            {
                var businessOwnerId = _httpContextAccessor.HttpContext?.User?
               .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("Assign failed: BusinessOwner ID not found in context.");
                    return false;
                }

                var businessOwner = await businessOwnerRepo.GetAsync(businessOwnerId);
                if (businessOwner == null)
                {
                    _logger.LogWarning($"Assign failed: No BusinessOwner found with ID {businessOwnerId}");
                    return false;
                }
                // Create order / order => high urgent / expected time = 0
                var order = await orderService.CreateOrder(_orderCreateVM, businessOwnerId); // should be await

           

                var orderRoute = await orderRouteRepository.GetOrderRouteByOrderID(order.Id);

                var route = await routeRepository.GetAsync(orderRoute.RouteID);
                // order->zone / search for all shipments with the same zone and be created

                // Fetch all shipments with Routes included
                var shipments = await shipmentRepository.GetAllAsync();


                Shipment shipment = null;
                foreach (var sh in shipments)
                {
                    // Condition 1: Check if shipment is not deleted
                    if (!sh.IsDeleted)
                    {
                        Console.WriteLine($"Shipment {sh.Id} is not deleted and is eligible for further checks.");
                    }
                    else
                    {
                        Console.WriteLine($"Shipment {sh.Id} is deleted and will be skipped.");
                        continue;
                    }

                    // Condition 2: Check if shipment has no routes or routes count is less than max consecutive deliveries
                    if (sh.Routes == null || sh.Routes.Count < sh.MaxConsecutiveDeliveries)
                    {
                        Console.WriteLine($"Shipment {sh.Id} has no routes or fewer than {sh.MaxConsecutiveDeliveries} routes, making it eligible.");
                    }
                    else
                    {
                        Console.WriteLine($"Shipment {sh.Id} has reached or exceeded its maximum consecutive deliveries ({sh.MaxConsecutiveDeliveries}).");
                        continue;
                    }

                    // Condition 3: Check if shipment state is Created or Assigned
                    if (sh.ShipmentState == ShipmentStateEnum.Created || sh.ShipmentState == ShipmentStateEnum.Assigned)
                    {
                        Console.WriteLine($"Shipment {sh.Id} is in {sh.ShipmentState} state, which is valid for assignment.");
                    }
                    else
                    {
                        Console.WriteLine($"Shipment {sh.Id} is in {sh.ShipmentState} state, which is not valid for assignment.");
                        continue;
                    }

                    // Condition 4: Check if shipment zone matches order zone
                    if (sh.zone == order.zone)
                    {
                        Console.WriteLine($"Shipment {sh.Id} is in the same zone as the order ({sh.zone}).");
                    }
                    else
                    {
                        Console.WriteLine($"Shipment {sh.Id} is in a different zone ({sh.zone}) than the order ({order.zone}).");
                        continue;
                    }

                    // Condition 5: Check if shipment's InTransiteBeginTime is after order's prepare time
                    if (sh.InTransiteBeginTime > DateTime.Now.Add(order.PrepareTime.Value))
                    {
                        Console.WriteLine($"Shipment {sh.Id} has an InTransiteBeginTime ({sh.InTransiteBeginTime}) after the order's prepare time ({DateTime.Now.Add(order.PrepareTime.Value)}).");
                    }
                    else
                    {
                        Console.WriteLine($"Shipment {sh.Id} has an InTransiteBeginTime ({sh.InTransiteBeginTime}) that is too early for the order's prepare time ({DateTime.Now.Add(order.PrepareTime.Value)}).");
                        continue;
                    }

                    // Condition 6: Check if order is HighUrgent and shipment's InTransiteBeginTime is at least 5 minutes after prepare time
                    if (order.OrderPriority == OrderPriorityEnum.HighUrgent)
                    {
                        if (sh.InTransiteBeginTime >= DateTime.Now.Add(order.PrepareTime.Value + TimeSpan.FromMinutes(5)))
        {
                            Console.WriteLine($"Shipment {sh.Id} meets the HighUrgent requirement with InTransiteBeginTime ({sh.InTransiteBeginTime}) at least 5 minutes after the order's prepare time ({DateTime.Now.Add(order.PrepareTime.Value)}).");
                        }
        else
                        {
                            Console.WriteLine($"Shipment {sh.Id} does not meet the HighUrgent requirement as its InTransiteBeginTime ({sh.InTransiteBeginTime}) is less than 5 minutes after the order's prepare time ({DateTime.Now.Add(order.PrepareTime.Value)}).");
                            continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Shipment {sh.Id} does not require HighUrgent timing check as the order priority is {order.OrderPriority}.");
                    }

                    // If all conditions pass, select this shipment and break
                    shipment = sh;
                    Console.WriteLine($"Shipment {sh.Id} meets all conditions and is selected.");
                    break;
                }

                //var shipment = await shipmentRepository
                // .GetList(sh => !sh.IsDeleted && (sh.Routes == null || sh.Routes.Count < sh.MaxConsecutiveDeliveries) &&
                // (sh.ShipmentState == ShipmentStateEnum.Created || sh.ShipmentState == ShipmentStateEnum.Assigned) &&
                // sh.zone == order.zone && 
                // sh.InTransiteBeginTime > DateTime.Now.Add(order.PrepareTime.Value)&&
                // // in this condition we check if the order is HighUrgent and if it is , get the shipments that their InTransiteBeginTime are only 5 min more than the prepare time 
                // (order.OrderPriority == OrderPriorityEnum.HighUrgent && sh.InTransiteBeginTime >= DateTime.Now.Add(order.PrepareTime.Value + TimeSpan.FromMinutes(5)))
                // )
                // .Include(s => s.Routes)
                // .FirstOrDefaultAsync();
                // 

                if (shipment != null)
                {
                    // Update Route => Add Shipment Id
                    route.ShipmentID = shipment.Id;
                    routeRepository.Update(route);
                    routeRepository.CustomSaveChanges();


                    // تعديل نهاية الشحنة لو المسار الجديد بعد المسار القديم
                    var lastRoute = shipment.Routes?.OrderByDescending(r => r.DestinationLang).ThenByDescending(r => r.DestinationLat).FirstOrDefault();

                    if (lastRoute != null)
                    {
                        // هنحسب المسافة التقريبية ما بين نقطة النهاية بتاعة آخر Route والنقطة الجديدة
                        double lastLat = lastRoute.DestinationLat;
                        double lastLng = lastRoute.DestinationLang;

                        double newLat = route.DestinationLat;
                        double newLng = route.DestinationLang;

                        // Calculate Distance Between Two Points
                        double distance = Math.Sqrt(Math.Pow(newLat - lastLat, 2) + Math.Pow(newLng - lastLng, 2));

                        // نحدد Threshold صغير نقول لو أقل من كده يبقى هو في نفس الاتجاه أو قريب
                        double threshold = 0.01;

                        if (distance > threshold)
                        {
                            Waypoint waypoint = new Waypoint() { ShipmentID = shipment.Id, Lang = shipment.EndLang, Lat = shipment.EndLat, Area = shipment.EndArea };
                            shipment.waypoints.Add(waypoint);

                            // المسافة بعيدة – يبقى ممكن نحدث الـ shipment ونخلي EndLocation بتاعه هو ده
                            shipment.EndLat = newLat;
                            shipment.EndLang = newLng;
                            shipment.EndArea = route.DestinationArea;
                        }
                        else
                        {
                            Waypoint waypoint = new Waypoint() { ShipmentID = shipment.Id, Lang = route.DestinationLang, Lat = route.DestinationLat, Area = route.DestinationArea };
                            shipment.waypoints.Add(waypoint);
                        }

                        // بترجع تعمل Update للـ shipment بعد التعديل
                    }

                    if(order.OrderPriority == OrderPriorityEnum.HighUrgent)
                    {
                        AssignOrderAutomaticallyAsync(businessOwnerId, order.Id, shipment);
                        // We can't change the time to the high urgent order prepare time as there other oreders in the shipment need more time
                        //shipment.InTransiteBeginTime = DateTime.Now.Add(order.PrepareTime.Value); 
                    }
                    shipmentRepository.Update(shipment);
                   shipmentRepository.CustomSaveChanges();
                    return true;

                }
                else
                {
                    TimeSpan? setWaitingTime;
                    // check order priorty
                    if (order.OrderPriority == OrderPriorityEnum.HighUrgent)
                    {
                        setWaitingTime = order.PrepareTime;

                    }
                    else if (order.OrderPriority == OrderPriorityEnum.Urgent)
                    {
                        setWaitingTime = order.PrepareTime + TimeSpan.FromMinutes(5);
                    }
                    else
                    {

                        setWaitingTime = order.PrepareTime + TimeSpan.FromMinutes(10);
                    }

                    await shipmentServices.CreateShipment(new AddShipmentVM
                    {
                        startTime = route.Start,
                        InTransiteBeginTime = DateTime.Now.Add(setWaitingTime.Value),
                        BeginningLang = route.OriginLang,
                        BeginningLat = route.OriginLat,
                        BeginningArea = route.OriginArea,
                        EndLang = route.DestinationLang,
                        EndLat = route.DestinationLat,
                        EndArea = route.DestinationArea,
                        zone = order.zone,
                        // The MaxConsecutiveDeliveries would be based on the total order waight
                        MaxConsecutiveDeliveries = 10
                    }, route);

                     return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while assigning Order to Rider.");
                return false;
            }

        }


        public async Task<bool> ViewOrder(int orderId, string riderId, bool isAccepted)
        {
            try
            {
                var order = await orderRepository.GetAsync(orderId);
                if (order == null || order.IsDeleted || order.State != OrderStateEnum.Pending || order.RiderID != riderId)
                {
                    return false;
                }

                var rider = await riderRepository.GetAsync(riderId);
                if (rider == null || rider.Status != RiderStatusEnum.Available)
                {
                    return false;
                }

                var orderRiderList = await orderRiderRepository.GetAllAsync();
                var orderRider = orderRiderList
                    .FirstOrDefault(or => or.OrderID == orderId && or.RiderID == riderId && !or.IsDeleted);

                if (orderRider == null)
                {
                    return false;
                }

                if (isAccepted)
                {
                    order.State = OrderStateEnum.Confirmed;
                    order.ModifiedBy = riderId;
                    order.ModifiedAt = DateTime.Now;

                    rider.Status = RiderStatusEnum.OnDelivery;

                    orderRider.ModifiedBy = riderId;
                    orderRider.ModifiedAt = DateTime.Now;

                    orderRepository.Update(order);
                    riderRepository.Update(rider);
                    orderRiderRepository.Update(orderRider);

                    await Task.Run(() =>
                    {
                        orderRepository.CustomSaveChanges();
                        riderRepository.CustomSaveChanges();
                        orderRiderRepository.CustomSaveChanges();
                    });

                    return true;
                }
                else
                {
                    order.State = OrderStateEnum.Pending;
                    //order.RiderID = "null";
                    order.ModifiedBy = riderId;
                    order.ModifiedAt = DateTime.Now;

                    rider.Status = RiderStatusEnum.Available;

                    orderRider.IsDeleted = true;
                    orderRider.ModifiedBy = riderId;
                    orderRider.ModifiedAt = DateTime.Now;

                    orderRepository.Update(order);
                    riderRepository.Update(rider);
                    orderRiderRepository.Update(orderRider);

                    await Task.Run(() =>
                    {
                        orderRepository.CustomSaveChanges();
                        riderRepository.CustomSaveChanges();
                        orderRiderRepository.CustomSaveChanges();
                    });

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Result> AssignOrderAutomaticallyAsync(string businessOwnerId, int orderId, Shipment shipment)
        {
            // Retrieve the business owner
            var businessOwner = await businessOwnerRepo.GetAsync(businessOwnerId);
            if (businessOwner == null)
                return Result.Failure("Business owner not found.");

            // Retrieve the order
            var order = await orderRepository.GetAsync(orderId);
            // Get available riders for the business owner
            var riders = await riderRepository.GetAvaliableRiders(businessOwnerId);
            

            // Filter riders based on vehicle status and weight capacity
            var filteredRiders = riders
                .Where(r => r.VehicleStatus == "Good")
                .ToList();

            if (!filteredRiders.Any())
                return Result.Failure("No available riders who can handle this order.");

            // Calculate distances and scores
            var distances = filteredRiders
                .Select(r => Haversine(35.5, 28.9, r.Lat, r.Lang))
                .ToList();

            var dMin = distances.Min();
            var dMax = distances.Max();

            var scoredRiders = filteredRiders
                .Select(r =>
                {
                    var distance = Haversine(35.5, 25.9, r.Lat, r.Lang);
                    var scoreDistance = dMax == dMin ? 100 : 100 * (dMax - distance) / (dMax - dMin);
                    var scoreExperience = GetExperienceScore(r.ExperienceLevel);
                    var scoreRating = r.Rating * 20;
                    var weightScore = GetMaxWeight(r.VehicleType);
                    var totalScore = scoreDistance + scoreExperience + scoreRating + weightScore;
                    return new { Rider = r, TotalScore = totalScore };
                })
                .ToList();

            // Select the best rider
            var bestRider = scoredRiders.OrderByDescending(x => x.TotalScore).FirstOrDefault();
            if (bestRider == null)
                return Result.Failure("No suitable rider found.");

            // Assign the order to the best rider
            order.RiderID = bestRider.Rider.UserID;
            order.State = OrderStateEnum.Confirmed;
            shipment.ShipmentState = ShipmentStateEnum.Assigned;
            shipmentRepository.Update(shipment);
            orderRepository.Update(order);
            orderRepository.CustomSaveChanges();

            return Result.Success("Order assigned successfully.");
        }

        private int GetMaxWeight(VehicleTypeEnum type)
        {
            switch (type)
            {
                case VehicleTypeEnum.Motorcycle: return 50;
                case VehicleTypeEnum.Car: return 100;
                case VehicleTypeEnum.Van: return 200;
                default: return 0;
            }
        }

        private double GetExperienceScore(float experienceLevel)
        {
            if (experienceLevel < 10) return 25; // Rookie
            if (experienceLevel < 20) return 50; // Experienced
            if (experienceLevel < 30) return 75; // Delivery Master
            return 100; // Leader
        }

        private double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c;
            return d;
        }

        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        public async Task StartTrial(string userId)
        {
            var owner = await businessOwnerRepo.GetAsync(userId);
            if (owner == null) throw new Exception("Owner not found");

            owner.SubscriptionType = SubscriptionTypeEnum.Trial;
            owner.SubscriptionStartDate = DateTime.Now;
            owner.SubscriptionEndDate = DateTime.Now.AddDays(7);

            businessOwnerRepo.Update(owner);
            businessOwnerRepo.CustomSaveChanges();
        }

        public async Task ActivatePaidAsync(string userId)
        {
            var owner = await businessOwnerRepo.GetAsync(userId);
            if (owner == null) throw new Exception("Owner not found");

            owner.SubscriptionType = SubscriptionTypeEnum.Paid;
            owner.SubscriptionStartDate = DateTime.Now;
            owner.SubscriptionEndDate = DateTime.Now.AddMonths(1);

            businessOwnerRepo.Update(owner);
            businessOwnerRepo.CustomSaveChanges();
        }

        public async Task RenewSubscriptionAsync(string userId)
        {
            var owner = await businessOwnerRepo.GetAsync(userId);
            if (owner == null) throw new Exception("Owner not found");

            if (owner.SubscriptionEndDate.HasValue && owner.SubscriptionEndDate > DateTime.Now)
                owner.SubscriptionEndDate = owner.SubscriptionEndDate.Value.AddMonths(1);
            else
                owner.SubscriptionEndDate = DateTime.Now.AddMonths(1);

            owner.SubscriptionType = SubscriptionTypeEnum.Paid;

            businessOwnerRepo.Update(owner);
            businessOwnerRepo.CustomSaveChanges();
        }

        private async Task NotifyRiderForShipmentConfirmation(string riderId, int shipmentId, string businessOwnerId)
        {
            try
            {
                // جلب بيانات الطلب والمسار
                var order = await orderRepository.GetAsync(shipmentId);
                if (order == null)
                {
                    _logger.LogWarning($"Notification failed: Order {shipmentId} not found.");
                    return;
                }

                var orderRoute = await orderRouteRepository.GetOrderRouteByOrderID(shipmentId);
                if (orderRoute == null)
                {
                    _logger.LogWarning($"Notification failed: Route for order {shipmentId} not found.");
                    return;
                }

                var route = await routeRepository.GetAsync(orderRoute.RouteID);
                if (route == null)
                {
                    _logger.LogWarning($"Notification failed: Route {orderRoute.RouteID} not found.");
                    return;
                }

                var message = new ShipmentConfirmation
                {
                    ShipmentId = shipmentId,
                    RiderId = riderId,
                    BusinessOwnerId = businessOwnerId,
                    ExpiryTime = DateTime.UtcNow.AddSeconds(30),
                    Status = ConfirmationStatus.Pending
                };

                // تخزين الرسالة مؤقتًا
                _confirmationStore[riderId] = message;

                // إعداد بيانات الإشعار
                var shipmentData = new
                {
                    shipmentId = shipmentId,
                    orderTitle = $"Order #{shipmentId}",
                    message = $"You have a new shipment #{shipmentId}. Please confirm within 30 seconds.",
                    expiry = message.ExpiryTime.ToString("o"), // ISO format
                    from = new
                    {
                        area = route.OriginArea,
                        lat = route.OriginLat,
                        lng = route.OriginLang
                    },
                    to = new
                    {
                        area = route.DestinationArea,
                        lat = route.DestinationLat,
                        lng = route.DestinationLang
                    },
                    pickupTime = order.PrepareTime?.ToString("o") ?? DateTime.UtcNow.ToString("o"),
                    orderPriority = order.OrderPriority.ToString() ?? "Normal"
                };

                // إرسال الإشعار عبر SignalR
                await _hubContext.Clients.User(riderId).SendAsync("ReceiveShipmentRequest", shipmentData);
                _logger.LogInformation($"Notification sent to rider {riderId} for shipment {shipmentId} with data: {JsonSerializer.Serialize(shipmentData)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to notify rider {riderId} for shipment {shipmentId}.");
            }
        }

        private void StartShipmentConfirmationTimer(string riderId, int shipmentId, string businessOwnerId)
        {
            var timer = new Timer(async _ =>
            {
                if (_confirmationStore.TryGetValue(riderId, out var message) && message.Status == ConfirmationStatus.Pending)
                {
                    // rider لم يرد خلال 30 ثانية → نعتبره رفض تلقائيًا
                    await HandleRiderShipmentTimeout(riderId, shipmentId, businessOwnerId);
                }
            }, null, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));
        }


        private async Task HandleRiderShipmentTimeout(string riderId, int shipmentId, string businessOwnerId)
        {
            _logger.LogWarning($"Rider {riderId} did not respond within 30 seconds for shipment {shipmentId}");

            // إعادة الشحن إلى الحالة Pending وإزالة Rider ID

            // يمكنك هنا أيضًا البحث عن Rider آخر تلقائيًا
            await AssignShipmentToAnotherRider(shipmentId, businessOwnerId);
        }


        private async Task AssignShipmentToAnotherRider(int shipmentId, string businessOwnerId)
        {
            // استدعاء خدمة التعيين الآلي مرة أخرى
            var result = await AssignOrderAutomaticallyAsync(businessOwnerId, shipmentId);

            if (result.IsSuccess)
            {
                _logger.LogInformation($"Shipment {shipmentId} reassigned to another rider.");
            }
            else
            {
                _logger.LogWarning($"No available riders for shipment {shipmentId} after timeout.");
            }
        }
    }
}