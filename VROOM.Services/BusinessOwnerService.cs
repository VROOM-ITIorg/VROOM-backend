using Hangfire;
using Hangfire.Server;
using Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NuGet.Protocol.Core.Types;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using ViewModels.Shipment;
using ViewModels.User;
using VROOM.Data;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Repositories;
using VROOM.Repository;
using VROOM.ViewModels;

namespace VROOM.Services
{

    public class CreateOrderWithAssignmentRequest
    {
        public OrderCreateViewModel Order { get; set; }
        public string AssignmentType { get; set; } // "Manual" or "Automatic"
        public string ? RiderId  { get; set; } // Required for manual assignment
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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserService _userService;
        private readonly OrderService orderService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserRepository _userRepository;
        private readonly IHubContext<RiderHub> _hubContext;
        private readonly IHubContext<OwnerHub> ownerContext;
            private readonly ConcurrentDictionary<string, ShipmentConfirmation> _confirmationStore;
        private readonly CustomerRepository customerRepository;

        public BusinessOwnerService(
            UserManager<User> _userManager,
            BusinessOwnerRepository _businessOwnerRepo,
            OrderRepository _orderRepository,
            RiderRepository _riderRepository,
            RoleManager<IdentityRole> roleManager,
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
            IHubContext<RiderHub> hubContext,
            IHubContext<OwnerHub> _ownerContext,
            ConcurrentDictionary<string, ShipmentConfirmation> confirmationStore,
            CustomerRepository _customerRepository 

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
            ownerContext = _ownerContext;
           customerRepository = _customerRepository;
            
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


        private async Task SendWhatsAppMessage(string phoneNumber, string userMessage)
        {
            string formatedPhoneNumber = NormalizePhoneNumber(phoneNumber);
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer ed2a766dcd8fdc7ba0dcb7958b263f03727be139e06a2ad294973eaf04d0a69f6bf58f4b4c810c93");
            var payload = new
            {
                phone = formatedPhoneNumber,
                message = userMessage
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.wassenger.com/v1/messages", content);
            response.EnsureSuccessStatusCode();


            _logger.LogInformation(response.Content.ToString());

        }

        private string NormalizePhoneNumber(string phoneNumber)
        {
            // Remove any non-digit characters (e.g., spaces, dashes)
            phoneNumber = Regex.Replace(phoneNumber ?? "", "[^0-9]", "");

            // Ensure the phone number starts with the country code
            if (!phoneNumber.StartsWith("+"))
            {
                // Example: Assume Egypt country code (+20) if the number starts with 0
                if (phoneNumber.StartsWith("0"))
                {
                    phoneNumber = $"+20{phoneNumber.Substring(1)}";
                }
                else
                {
                    // Handle other cases or throw an exception if the number is invalid
                    throw new ArgumentException("Invalid phone number format. Please provide a number with a country code or in a valid format.");
                }
            }

            return phoneNumber;
        }
        public async Task<Result<RiderVM>> CreateRiderAsync(RiderRegisterRequest request, string BusinessID)
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

                var businessOwner = await businessOwnerRepo.GetAsync(BusinessID);
                if (businessOwner == null)
                {
                    _logger.LogWarning($"Assign failed: No BusinessOwner found with ID {BusinessID}");
                 
                }

                var user = new User
                {
                    UserName = request.Email,
                    Email = request.Email,      
                    Name = request.Name,
                    PhoneNumber = request.phoneNumber,
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
                    BusinessID = BusinessID,
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
                await SendWhatsAppMessage(user.PhoneNumber, $"Greating, You are a rider for {businessOwner.User.Name} Business now, try to login with your username: {rider.User.UserName} and password : {request.Password} , You are his slave now congrates!😊");

                var result = new RiderVM
                {
                    UserID = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    phoneNumber = user.PhoneNumber,
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

        public async Task<Result<RiderVM>> UpdateRiderAsync(RiderUpdateRequest request, string BusinessID, string riderUserId)
        {
            _logger.LogInformation("Updating rider with email: {Email}", request.Email);

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var user = await userManager.FindByIdAsync(riderUserId);
                if (user == null)
                {
                    _logger.LogWarning("No user found with ID: {UserId}", riderUserId);
                    return Result<RiderVM>.Failure("User not found.");
                }

                var businessOwner = await businessOwnerRepo.GetAsync(BusinessID);
                if (businessOwner == null)
                {
                    _logger.LogWarning($"Update failed: No BusinessOwner found with ID {BusinessID}");
                    return Result<RiderVM>.Failure("Business owner not found.");
                }

                // Check if email is being updated to a new email that's already in use
                if (!string.IsNullOrEmpty(request.Email) && user.Email != request.Email)
                {
                    var existingUser = await userManager.FindByEmailAsync(request.Email);
                    if (existingUser != null)
                    {
                        _logger.LogWarning("A user with this email already exists: {Email}", request.Email);
                        return Result<RiderVM>.Failure("A user with this email already exists.");
                    }
                    user.Email = request.Email;
                    user.UserName = request.Email; // Update UserName if Email is provided
                }

                // Update user properties only if provided
                if (!string.IsNullOrEmpty(request.Name))
                    user.Name = request.Name;
                if (!string.IsNullOrEmpty(request.phoneNumber))
                    user.PhoneNumber = request.phoneNumber;
                if (!string.IsNullOrEmpty(request.ProfilePicture))
                    user.ProfilePicture = request.ProfilePicture;

                _logger.LogInformation("Attempting to update user: {Email}", user.Email);
                var updateResult = await userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    var errorMessages = string.Join(",", updateResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to update user: {Email}. Errors: {Errors}", user.Email, errorMessages);
                    return Result<RiderVM>.Failure(errorMessages);
                }

                // Find and update rider
                var rider = await riderRepository.GetAsync(riderUserId);
                if (rider == null)
                {
                    _logger.LogWarning("No rider found for user ID: {UserId}", riderUserId);
                    return Result<RiderVM>.Failure("Rider not found.");
                }

                // Update rider properties only if provided
                if (!string.IsNullOrEmpty(BusinessID))
                    rider.BusinessID = BusinessID;
                if (request.VehicleType != default)
                    rider.VehicleType = request.VehicleType;
                if (!string.IsNullOrEmpty(request.VehicleStatus))
                    rider.VehicleStatus = request.VehicleStatus;
                if (request.ExperienceLevel != default)
                    rider.ExperienceLevel = request.ExperienceLevel;
                if (request.Location != null)
                {
                    // Update only provided location fields
                    if (request.Location.Lat != default)
                        rider.Lat = request.Location.Lat;
                    if (request.Location.Lang != default)
                        rider.Lang = request.Location.Lang;
                    if (!string.IsNullOrEmpty(request.Location.Area))
                        rider.Area = request.Location.Area;
                }

                _logger.LogInformation("Updating rider in repository for user: {Email}", user.Email);
                riderRepository.Update(rider);
                riderRepository.CustomSaveChanges();

                var result = new RiderVM
                {
                    UserID = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    phoneNumber = user.PhoneNumber,
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
                _logger.LogInformation("Rider updated successfully: {Email}", user.Email);

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




        public async Task<Result<CustomerVM>> CreateCustomerAsync(CustomerRegisterRequest request)
        {
            _logger.LogInformation("Creating customer with email: {Email}", request.Email);

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var businessOwnerId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(businessOwnerId))
                    {
                        _logger.LogWarning("Failed to create customer: Business Owner ID not found in token.");
                        return Result<CustomerVM>.Failure("Business Owner ID not found in token.");
                    }

                    var businessOwner = await businessOwnerRepo.GetAsync(businessOwnerId);
                    if (businessOwner == null)
                    {
                        _logger.LogWarning("Failed to create customer: Business Owner with ID {BusinessOwnerId} not found.", businessOwnerId);
                        return Result<CustomerVM>.Failure("Business Owner not found.");
                    }

                    var roles = await userManager.GetRolesAsync(businessOwner.User);
                    if (!roles.Contains(RoleConstants.BusinessOwner))
                    {
                        _logger.LogWarning("Failed to create customer: Caller with ID {BusinessOwnerId} is not a Business Owner.", businessOwnerId);
                        return Result<CustomerVM>.Failure("Caller is not a Business Owner.");
                    }

                    var existingUser = await userManager.FindByEmailAsync(request.Email);
                    if (existingUser != null)
                    {
                        _logger.LogWarning("A user with this email already exists: {Email}", request.Email);
                        return Result<CustomerVM>.Failure("A user with this email already exists.");
                    }

                    var user = new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = request.Email,
                        Email = request.Email,
                        Name = request.Name,
                        PhoneNumber = request.PhoneNumber,
                        ProfilePicture = request.ProfilePicture,
                        Address = new Address
                        {
                            Lat = request.Location?.Lat ?? 0,
                            Lang = request.Location?.Lang ?? 0,
                            Area = request.Location?.Area
                        }
                    };

                    _logger.LogInformation("Attempting to create user: {Email}", request.Email);
                    var creationResult = await userManager.CreateAsync(user, request.Password);
                    if (!creationResult.Succeeded)
                    {
                        var errorMessages = string.Join(",", creationResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to create user: {Email}. Errors: {Errors}", request.Email, errorMessages);
                        return Result<CustomerVM>.Failure(errorMessages);
                    }

                    _logger.LogInformation("User created successfully: {Email}", request.Email);

                    var role = RoleConstants.Customer;
                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        _logger.LogInformation("Role {Role} does not exist. Creating role...", role);
                        await _roleManager.CreateAsync(new IdentityRole(role));
                    }

                    _logger.LogInformation("Assigning role {Role} to user: {Email}", role, request.Email);
                    await userManager.AddToRoleAsync(user, role);

                    // Ensure Address has the correct UserID
                    user.Address.UserID = user.Id;

                    var customer = new Customer
                    {
                        UserID = user.Id,
                        User = user,
                     
                    };

                    _logger.LogInformation("Adding customer to the repository for user: {Email}", request.Email);
                    // Remove redundant save for User since userManager.CreateAsync already persists it
                    // _userRepository.Add(user);
                    // await _userRepository.CustomSaveChanges();

                    customerRepository.Add(customer);
                    await customerRepository.CustomSaveChangesAsync();

                    // Enqueue the Hangfire background job
                    BackgroundJob.Enqueue(() => LogCustomerCreation(user.Id, user.Email, user.PhoneNumber));

                    var customerVM = new CustomerVM
                    {
                        UserID = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Location = user.Address != null ? new LocationDto
                        {
                            Lat = user.Address.Lat,
                            Lang = user.Address.Lang,
                            Area = user.Address.Area
                        } : null
                    };

                    scope.Complete();

                    _logger.LogInformation("Customer created successfully: {Email}", request.Email);
                    return Result<CustomerVM>.Success(customerVM);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while creating customer with email {Email}.", request.Email);
                    return Result<CustomerVM>.Failure($"An error occurred while creating the customer: {ex.Message}");
                }
            }
        }

        public void LogCustomerCreation(string userId, string email, string phoneNumber)
        {
            _logger.LogInformation("Background task: Customer created with ID {UserId}, email {Email}, and phone {PhoneNumber} on {DateTime}.", userId, email, phoneNumber, DateTime.Now);
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

        public async Task<string> GetBusinessOwnerIdForRiderAsync(string riderId)
        {
            if (string.IsNullOrEmpty(riderId))
            {
                throw new ArgumentException("Rider ID cannot be null or empty");
            }

            var rider = await riderRepository.GetAsync(riderId);

            if (rider == null)
            {
                throw new KeyNotFoundException($"Rider with ID {riderId} not found");
            }

            if (string.IsNullOrEmpty(rider.BusinessID))
            {
                throw new InvalidOperationException($"Rider {riderId} is not assigned to a business owner");
            }

            return rider.BusinessID;
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

                // Temporarily assign the order to the rider
                order.RiderID = riderId;
                order.State = OrderStateEnum.Pending;
                order.ModifiedBy = businessOwnerId;
                order.ModifiedAt = DateTime.Now;
                orderRepository.Update(order);
                orderRepository.CustomSaveChanges();

                // Notify rider and wait for response
                var notificationSent = await NotifyRiderWithRetry(riderId, orderId, businessOwnerId, maxRetries: 2, retryDelay: TimeSpan.FromSeconds(5));
                if (!notificationSent)
                {
                    _logger.LogWarning($"Failed to notify rider {riderId} for order {orderId} after retries.");
                    // Reset order state
                    order.RiderID = null;
                    order.State = OrderStateEnum.Created;
                    orderRepository.Update(order);
                     orderRepository.CustomSaveChanges();
                    return false;
                }

                // Wait for rider response (accept/reject)
                var confirmation = await WaitForRiderResponse(riderId, orderId, timeoutSeconds: 30);
                if (confirmation != ConfirmationStatus.Accepted)
                {
                    string message = confirmation == ConfirmationStatus.Rejected
                        ? $"Rider {rider.User.Name} rejected order {order.Title}."
                        : $"Rider {rider.User.Name}  did not respond to order  {order.Title}.";
                    _logger.LogInformation(message);

                    // Notify business owner via OwnerHub
                    await ownerContext.Clients.User(businessOwnerId).SendAsync("ReceiveNotification", message);


                    // Reset order state
                    order.RiderID = null;
                    order.State = OrderStateEnum.Created;
                    orderRepository.Update(order);
           
                    orderRepository.CustomSaveChanges();
                    return false;
                }

                // Rider accepted the order
                string successMessage = $"Rider {rider.User.Name} accepted order {order.Title}.";
                _logger.LogInformation(successMessage);
                // Notify business owner via SignalR
                await ownerContext.Clients.User(businessOwnerId).SendAsync("ReceiveNotification", successMessage);
                // Proceed with assignment
                await orderService.UpdateOrderState(order.Id, OrderStateEnum.Confirmed, riderId, businessOwnerId);

                rider.Status = RiderStatusEnum.OnDelivery;
                riderRepository.Update(rider);
                // Create or update shipment
                var orderRoute =  orderRouteRepository.GetOrderRouteByOrderID(orderId);
                var route = await routeRepository.GetAsync(orderRoute.RouteID);

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

                    // Update shipment end location if the new route extends beyond the last route
                    var lastRoute = shipment.Routes?.OrderByDescending(r => r.DestinationLang).ThenByDescending(r => r.DestinationLat).FirstOrDefault();

                    if (lastRoute != null)
                    {
                        double lastLat = lastRoute.DestinationLat;
                        double lastLng = lastRoute.DestinationLang;
                        double newLat = route.DestinationLat;
                        double newLng = route.DestinationLang;

                        double distance = Math.Sqrt(Math.Pow(newLat - lastLat, 2) + Math.Pow(newLng - lastLng, 2));
                        double threshold = 0.01;

                        if (distance > threshold)
                        {
                            Waypoint waypoint = new Waypoint
                            {
                                ShipmentID = shipment.Id,
                                Lang = shipment.EndLang,
                                Lat = shipment.EndLat,
                                Area = shipment.EndArea,
                                orderId = orderId // Set OrderId

                            };
                            shipment.waypoints.Add(waypoint);
                            shipment.EndLat = newLat;
                            shipment.EndLang = newLng;
                            shipment.EndArea = route.DestinationArea;
                        }
                        else
                        {
                            Waypoint waypoint = new Waypoint
                            {
                                ShipmentID = shipment.Id,
                                Lang = route.DestinationLang,
                                Lat = route.DestinationLat,
                                Area = route.DestinationArea,
                                orderId = orderId // Set OrderId
                            };
                            shipment?.waypoints?.Add(waypoint);
                        }

                        shipmentRepository.Update(shipment);
                        shipmentRepository.CustomSaveChanges();
                    }

                    // Update shipment state to Assigned
                    shipment.ShipmentState = ShipmentStateEnum.Assigned;
                    
                    shipmentRepository.Update(shipment);
                    shipmentRepository.CustomSaveChanges();
                }
                else
                {
                    // Create new shipment
                    shipmentServices.CreateShipment(new AddShipmentVM
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
                        
                        
                    });
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


        public class CustomerRegisterRequest
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string Password { get; set; }
            public string ProfilePicture { get; set; }
            public LocationDto Location { get; set; }
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
                    State = order.State.ToString(),
                    RiderName = order.Rider.User.Name,
                    CustomerName = order.Customer.User.Name,
                    BusinessOwner = order.Rider.BusinessOwner.User.Name,
                    Priority = order.OrderPriority.ToString(),
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
                            PhoneNumber = customer.PhoneNumber, // Added
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
                _logger.LogError(ex, "An error occurred while retrieving customers for Business Owner with ID {BusinessOwnerId}.");
                return Result<List<CustomerVM>>.Failure("An error occurred while retrieving customers.");
            }
        }


        public async Task<Result<List<CustomerVM>>> GetAllCustomers()
        {
            try
            {
                // Extract Business Owner ID from the HTTP context
                var businessOwnerId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("Failed to retrieve all customers: Business Owner ID not found in token.");
                    return Result<List<CustomerVM>>.Failure("Business Owner ID not found in token.");
                }

                // Verify Business Owner exists
                var businessOwner = await businessOwnerRepo.GetAsync(businessOwnerId);
                if (businessOwner == null)
                {
                    _logger.LogWarning("Failed to retrieve all customers: Business Owner with ID {BusinessOwnerId} not found.", businessOwnerId);
                    return Result<List<CustomerVM>>.Failure("Business Owner not found.");
                }

                // Verify the caller is a Business Owner
                var roles = await userManager.GetRolesAsync(businessOwner.User);
                if (!roles.Contains(RoleConstants.BusinessOwner))
                {
                    _logger.LogWarning("Failed to retrieve all customers: Caller with ID {BusinessOwnerId} is not a Business Owner.", businessOwnerId);
                    return Result<List<CustomerVM>>.Failure("Caller is not a Business Owner.");
                }

                // Fetch all customers from the database
                var customers = await customerRepository.GetAllAsync();

                // Map customers to CustomerVM
                var customerVMs = new List<CustomerVM>();
                foreach (var customer in customers)
                {
                    var user = await userManager.FindByIdAsync(customer.UserID);
                    if (user != null)
                    {
                        var customerRoles = await userManager.GetRolesAsync(user);
                        if (customerRoles.Contains(RoleConstants.Customer))
                        {
                            customerVMs.Add(new CustomerVM
                            {
                                UserID = customer.UserID,
                                Name = user.Name,
                                Email = user.Email,
                                PhoneNumber = user.PhoneNumber,
                                Location = user.Address != null ? new LocationDto
                                {
                                    Lat = user.Address.Lat,
                                    Lang = user.Address.Lang,
                                    Area = user.Address.Area
                                } : null
                            });
                        }
                    }
                }

                _logger.LogInformation("Successfully retrieved {CustomerCount} customers from the database for Business Owner with ID {BusinessOwnerId}.", customerVMs.Count, businessOwnerId);
                return Result<List<CustomerVM>>.Success(customerVMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving all customers for Business Owner with ID {BusinessOwnerId}.");
                return Result<List<CustomerVM>>.Failure("An error occurred while retrieving all customers.");
            }
        }

        
        public async Task <bool> PrepareOrder(OrderCreateViewModel _orderCreateVM)
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



                var orderRoute =  orderRouteRepository.GetOrderRouteByOrderID(order.Id);

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
                    //if (sh.InTransiteBeginTime > DateTime.Now.Add(order.PrepareTime.Value))
                    //{
                    //    Console.WriteLine($"Shipment {sh.Id} has an InTransiteBeginTime ({sh.InTransiteBeginTime}) after the order's prepare time ({DateTime.Now.Add(order.PrepareTime.Value)}).");
                    //}
                    //else
                    //{
                    //    Console.WriteLine($"Shipment {sh.Id} has an InTransiteBeginTime ({sh.InTransiteBeginTime}) that is too early for the order's prepare time ({DateTime.Now.Add(order.PrepareTime.Value)}).");
                    //    continue;
                    //}

                    // Condition 6: Check if order is HighUrgent and shipment's InTransiteBeginTime is at least 5 minutes after prepare time
                    //if (order.OrderPriority == OrderPriorityEnum.HighUrgent)
                    //{
                    //    if (sh.InTransiteBeginTime >= DateTime.Now.Add(order.PrepareTime.Value + TimeSpan.FromMinutes(5)))
                    //    {
                    //        Console.WriteLine($"Shipment {sh.Id} meets the HighUrgent requirement with InTransiteBeginTime ({sh.InTransiteBeginTime}) at least 5 minutes after the order's prepare time ({DateTime.Now.Add(order.PrepareTime.Value)}).");
                    //    }
                    //    else
                    //    {
                    //        Console.WriteLine($"Shipment {sh.Id} does not meet the HighUrgent requirement as its InTransiteBeginTime ({sh.InTransiteBeginTime}) is less than 5 minutes after the order's prepare time ({DateTime.Now.Add(order.PrepareTime.Value)}).");
                    //        continue;
                    //    }
                    //}
                    //else
                    //{
                    //    Console.WriteLine($"Shipment {sh.Id} does not require HighUrgent timing check as the order priority is {order.OrderPriority}.");
                    //}

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

                    }

                    if (order.OrderPriority == OrderPriorityEnum.HighUrgent)
                    {
                       await AssignOrderAutomaticallyAsync(businessOwnerId, order.Id, shipment);
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

                    shipment = await shipmentServices.CreateShipment(new AddShipmentVM
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
                        MaxConsecutiveDeliveries = 10,
                        OrderIds = [order.Id]
                    });
                    await AssignOrderAutomaticallyAsync(businessOwnerId, order.Id, shipment);

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
            try
            {
                var businessOwner = await businessOwnerRepo.GetAsync(businessOwnerId);
                if (businessOwner == null)
                {
                    _logger.LogWarning($"Business owner with ID {businessOwnerId} not found.");
                    return Result.Failure("Business owner not found.");
                }

                var order = await orderRepository.GetAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    _logger.LogWarning($"Order with ID {orderId} not found or deleted.");
                    return Result.Failure("Order not found or deleted.");
                }

                var orderRoute =  orderRouteRepository.GetOrderRouteByOrderID(orderId);
                if (orderRoute == null)
                {
                    _logger.LogWarning($"Route for order {orderId} not found.");
                    return Result.Failure("Route not found.");
                }

                var route = await routeRepository.GetAsync(orderRoute.RouteID);
                if (route == null)
                {
                    _logger.LogWarning($"Route with ID {orderRoute.RouteID} not found.");
                    return Result.Failure("Route not found.");
                }

                int maxCycles = 3;
                int currentCycle = 0;
                var attemptedRiders = new HashSet<string>();
                var rejectedRiders = new HashSet<string>();
                TimeSpan delayBetweenCycles = TimeSpan.FromSeconds(10);

                while (currentCycle < maxCycles)
                {
                    // Refresh order state
                    order = await orderRepository.GetAsync(orderId);
                    if (order == null || order.IsDeleted || order.State != OrderStateEnum.Created)
                    {
                        _logger.LogInformation($"Order {orderId} is no longer pending or was deleted. Stopping assignment.");
                        return Result.Failure("Order is no longer pending or was deleted.");
                    }

                    var riders = await riderRepository.GetAvaliableRiders(businessOwnerId);
                    var filteredRiders = riders
                        .Where(r => !rejectedRiders.Contains(r.UserID) && r.VehicleStatus == "Good" && IsVehicleSuitable(r.VehicleType, order))
                        .ToList();

                    if (!filteredRiders.Any())
                    {
                        _logger.LogWarning($"No suitable riders for order {orderId} in cycle {currentCycle + 1}.");
                        return Result.Failure("No suitable riders found for this order.");
                    }

                    var scoredRiders = filteredRiders
                        .Select(r =>
                        {
                            var distance = Haversine(route.OriginLat, route.OriginLang, r.Lat, r.Lang);
                            var scoreDistance = CalculateDistanceScore(distance, filteredRiders, route.OriginLat, route.OriginLang) * 0.5;
                            var scoreExperience = GetExperienceScore(r.ExperienceLevel) * 0.2;
                            var scoreRating = r.Rating * 20 * 0.3;
                            var totalScore = scoreDistance + scoreExperience + scoreRating;
                            return new { Rider = r, TotalScore = totalScore, Distance = distance };
                        })
                        .OrderByDescending(x => x.TotalScore)
                        .ToList();

                    foreach (var scoredRider in scoredRiders)
                    {
                        if (attemptedRiders.Contains(scoredRider.Rider.UserID))
                            continue;

                        var rider = scoredRider.Rider;
                        attemptedRiders.Add(rider.UserID);

                        _logger.LogInformation($"Attempting to assign order {orderId} to rider {rider.UserID} (Cycle {currentCycle + 1}/{maxCycles}, Score: {scoredRider.TotalScore:F2}).");

                        // Assign order temporarily
                        order.RiderID = rider.UserID;
                        order.State = OrderStateEnum.Pending;
                        order.ModifiedBy = businessOwnerId;
                        order.ModifiedAt = DateTime.Now;
                        orderRepository.Update(order);
                        orderRepository.CustomSaveChanges();

                        var notificationSent = await NotifyRiderWithRetry(rider.UserID, orderId, businessOwnerId, maxRetries: 2, retryDelay: TimeSpan.FromSeconds(5));
                        if (!notificationSent)
                        {
                            _logger.LogWarning($"Failed to notify rider {rider.UserID} for order {orderId} after retries.");
                            rejectedRiders.Add(rider.UserID);
                            continue;
                        }

                        var confirmation = await WaitForRiderResponse(rider.UserID, orderId, timeoutSeconds: 30);
                        if (confirmation == ConfirmationStatus.Accepted)
                        {
                            // Double-check order state
                            order = await orderRepository.GetAsync(orderId);
                            if (order.State != OrderStateEnum.Pending)
                            {
                                _logger.LogInformation($"Order {orderId} is no longer pending. Stopping assignment.");
                                return Result.Success("Order assigned successfully.");
                            }

                            // Update shipment state
                            shipment.ShipmentState = ShipmentStateEnum.Assigned;
                            order.State = OrderStateEnum.Confirmed;
                            orderRepository.Update(order);
                            orderRepository.CustomSaveChanges();
                            shipmentRepository.Update(shipment);
                            shipmentRepository.CustomSaveChanges();

                            _logger.LogInformation($"Order {orderId} assigned to rider {rider.UserID} successfully.");
                            await NotifyRiderConfirmation(rider.UserID, orderId, true, "Order assigned successfully.");
                            return Result.Success("Order assigned successfully.");
                        }
                        else
                        {
                            _logger.LogInformation($"Rider {rider.UserID} {(confirmation == ConfirmationStatus.Rejected ? "rejected" : "did not respond to")} order {orderId}.");
                            rejectedRiders.Add(rider.UserID);
                            await NotifyRiderConfirmation(rider.UserID, orderId, false, confirmation == ConfirmationStatus.Rejected ? "Order rejected." : "Response timed out.");

                            // Reset order for next rider
                            order.RiderID = null;
                            order.State = OrderStateEnum.Pending;
                            order.ModifiedBy = businessOwnerId;
                            order.ModifiedAt = DateTime.Now;
                            orderRepository.Update(order);
                            orderRepository.CustomSaveChanges();
                            continue;
                        }
                    }

                    attemptedRiders.Clear();
                    currentCycle++;
                    if (currentCycle < maxCycles)
                    {
                        _logger.LogInformation($"No rider accepted order {orderId} in cycle {currentCycle}. Waiting {delayBetweenCycles.TotalSeconds} seconds.");
                        await Task.Delay(delayBetweenCycles);
                    }
                }

                _logger.LogWarning($"Failed to assign order {orderId} after {maxCycles} cycles.");
                return Result.Failure("No rider accepted the order after maximum attempts.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning order {orderId} for business owner {businessOwnerId}.");
                return Result.Failure("An error occurred while assigning the order.");
            }
        }
        private async Task<ConfirmationStatus> WaitForRiderResponse(string riderId, int orderId, int timeoutSeconds)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(timeoutSeconds))
            {
                if (_confirmationStore.TryGetValue(riderId, out var confirmation) &&
                    confirmation.ShipmentId == orderId &&
                    confirmation.Status != ConfirmationStatus.Pending)
                {
                    return confirmation.Status;
                }
                await Task.Delay(1000); // Check every 1000ms
            }
            return ConfirmationStatus.Pending;
        }
        // Helper methods
        private bool IsVehicleSuitable(VehicleTypeEnum vehicleType, Order order)
        {
            var requiredWeight = order.Weight;
            switch (vehicleType)
            {
                case VehicleTypeEnum.Motorcycle: return requiredWeight <= 50;
                case VehicleTypeEnum.Car: return requiredWeight <= 100;
                case VehicleTypeEnum.Van: return requiredWeight <= 200;
                default: return false;
            }
        }

        private async Task<bool> NotifyRiderWithRetry(string riderId, int orderId, string businessOwnerId, int maxRetries, TimeSpan retryDelay)
        {
            int attempt = 0;
            while (attempt <= maxRetries)
            {
                try
                {
                    await NotifyRiderForShipmentConfirmation(riderId, orderId, businessOwnerId);
                    _logger.LogInformation($"Notification sent to rider {riderId} for order {orderId} on attempt {attempt + 1}.");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to notify rider {riderId} for order {orderId} on attempt {attempt + 1}.");
                    attempt++;
                    if (attempt <= maxRetries)
                    {
                        _logger.LogInformation($"Retrying notification for rider {riderId} after {retryDelay.TotalSeconds} seconds.");
                        await Task.Delay(retryDelay);
                    }
                }
            }
            return false;
        }

        public async Task NotifyRiderConfirmation(string riderId, int orderId, bool success, string message)
        {
            try
            {
                await _hubContext.Clients.User(riderId).SendAsync("ShipmentResponseConfirmation", new
                {
                    ShipmentId = orderId,
                    Success = success,
                    Message = message
                });
                _logger.LogInformation($"Confirmation sent to rider {riderId} for order {orderId}: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send confirmation to rider {riderId} for order {orderId}.");
            }
        }

        private double CalculateDistanceScore(double distance, List<Rider> riders, double originLat, double originLang)
        {
            var distances = riders.Select(r => Haversine(originLat, originLang, r.Lat, r.Lang)).ToList();
            var dMin = distances.Min();
            var dMax = distances.Max();

            if (dMax == dMin)
                return 100;

            return 100 * (dMax - distance) / (dMax - dMin);
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
                var order = await orderRepository.GetAsync(shipmentId);
                if (order == null)
                {
                    _logger.LogWarning($"Notification failed: Order {shipmentId} not found.");
                    return;
                }

                var orderRoute =  orderRouteRepository.GetOrderRouteByOrderID(shipmentId);
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

                _confirmationStore[riderId] = message;

                // Prepare Notification
                var shipmentData = new
                {
                    ShipmentId = shipmentId,
                    orderTitle = $"Shipment #{order.Title}",
                    orderDetails = order.Details,
                    message = $"You have a new shipment. Please confirm within 30 seconds.",
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
                    pickupTime = order.PrepareTime?.ToString() ?? DateTime.UtcNow.ToString("o"),
                    orderPriority = order.OrderPriority.ToString() ?? "Normal",
                    RiderId = riderId
                };

                await _hubContext.Clients.User(riderId).SendAsync("ReceiveShipmentRequest", shipmentData);
                _logger.LogInformation($"Notification sent to rider {riderId} for shipment {shipmentId} with data: {JsonSerializer.Serialize(shipmentData)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to notify rider {riderId} for shipment {shipmentId}.");
            }
        }



        //The function for requert to
        public async Task<Result<string>> CreateOrderAndAssignAsync(CreateOrderWithAssignmentRequest request)
{
    try
    {
        var businessOwnerId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(businessOwnerId))
        {
            _logger.LogWarning("Failed to create order: Business Owner ID not found in token.");
            return Result<string>.Failure("Business Owner ID not found in token.");
        }

        var businessOwner = await businessOwnerRepo.GetAsync(businessOwnerId);
        if (businessOwner == null)
        {
            _logger.LogWarning("Failed to create order: Business Owner with ID {BusinessOwnerId} not found.", businessOwnerId);
            return Result<string>.Failure("Business Owner not found.");
        }

        var roles = await userManager.GetRolesAsync(businessOwner.User);
        if (!roles.Contains(RoleConstants.BusinessOwner))
        {
            _logger.LogWarning("Failed to create order: Caller with ID {BusinessOwnerId} is not a Business Owner.", businessOwnerId);
            return Result<string>.Failure("Caller is not a Business Owner.");
        }

               

                // Handle assignment based on type
                bool assignmentSuccess = false;
        if (request.AssignmentType?.ToLower() == "manual" && !string.IsNullOrEmpty(request.RiderId))
        {

           var order = await orderService.CreateOrder(request.Order, businessOwnerId);

                    if (order == null)
                    {
                        _logger.LogWarning("Failed to create order for Business Owner {BusinessOwnerId}.", businessOwnerId);
                        return Result<string>.Failure("Failed to create order.");
                    }

                    assignmentSuccess = await AssignShipmentToRiderAsync(order.Id, request.RiderId);
            if (!assignmentSuccess)
            {
                _logger.LogWarning("Manual assignment failed for order {OrderId} to rider {RiderId}.", order.Id, request.RiderId);
                return Result<string>.Failure("Manual assignment failed.");
            }
        }
        else if (request.AssignmentType?.ToLower() == "automatic")
        {

                    var result = await PrepareOrder(request.Order);
            if (!result)
            {
                _logger.LogWarning("Automatic assignment failed for order {OrderId}: {Error}", result);
                        return Result<string>.Failure("error in Automatic");
            }
            assignmentSuccess = true;
        }
        else
        {
            _logger.LogWarning("Invalid or missing assignment type for order {OrderId}.");
            return Result<string>.Failure("Invalid or missing assignment type. Use 'Manual' or 'Automatic'.");
        }

                //_logger.LogInformation("Order {OrderId} created and assigned successfully by Business Owner {BusinessOwnerId}.", businessOwnerId);
                //return Result<string>.Success($"Order { and assigned successfully.");

               return Result<string>.Success($"Automatic assignment done ");

            }
            catch (Exception ex)
    {
        _logger.LogError(ex, "An error occurred while creating and assigning order for Business Owner {BusinessOwnerId}.", _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        return Result<string>.Failure($"An error occurred: {ex.Message}");
    }
}


    
        //private void StartShipmentConfirmationTimer(string riderId, int shipmentId, string businessOwnerId)
        //{
        //    var timer = new Timer(async _ =>
        //    {
        //        if (_confirmationStore.TryGetValue(riderId, out var message) && message.Status == ConfirmationStatus.Pending)
        //        {
        //            await HandleRiderShipmentTimeout(riderId, shipmentId, businessOwnerId);
        //        }
        //    }, null, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));
        //}


        //private async Task HandleRiderShipmentTimeout(string riderId, int shipmentId, string businessOwnerId)
        //{
        //    _logger.LogWarning($"Rider {riderId} did not respond within 30 seconds for shipment {shipmentId}");

        //    // Remove the confirmation to prevent re-processing
        //    _confirmationStore.TryRemove(riderId, out _);

        //    // Check if we should try reassigning
        //    var order = await orderRepository.GetAsync(shipmentId);
        //    if (order == null || order.IsDeleted || order.State != OrderStateEnum.Pending)
        //    {
        //        _logger.LogWarning($"Cannot reassign shipment {shipmentId}: Invalid order state or order not found.");
        //        return;
        //    }

        //    await AssignShipmentToAnotherRider(shipmentId, businessOwnerId);
        //}


        //private async Task AssignShipmentToAnotherRider(int orderId, string businessOwnerId)
        //{
        //    var order = await orderRepository.GetAsync(orderId);
        //    if (order == null || order.IsDeleted)
        //    {
        //        _logger.LogWarning($"Cannot reassign order {orderId}: Order not found or deleted.");
        //        return;
        //    }

        //    var shipmentId = order.OrderRoute?.Route?.ShipmentID;
        //    if (!shipmentId.HasValue)
        //    {
        //        _logger.LogWarning($"Cannot reassign order {orderId}: Shipment ID not found.");
        //        return;
        //    }

        //    var shipment = await shipmentRepository.GetAsync(shipmentId.Value);
        //    if (shipment == null)
        //    {
        //        _logger.LogWarning($"Cannot reassign order {orderId}: Shipment {shipmentId} not found.");
        //        return;
        //    }

        //    var result = await AssignOrderAutomaticallyAsync(businessOwnerId, orderId, shipment);

        //    if (result.IsSuccess)
        //    {
        //        _logger.LogInformation($"Shipment {shipmentId} reassigned to another rider.");
        //    }
        //    else
        //    {
        //        _logger.LogWarning($"No available riders for shipment {shipmentId} after timeout.");
        //    }
        //}


    } 


}