using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using ViewModels.User;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Repositories;
using VROOM.Repository;
using VROOM.ViewModels;

namespace VROOM.Services
{
    public class BusinessOwnerService
    {
        //private readonly MyDbContext businessOwnerRepo;
        private readonly ILogger<BusinessOwnerService> _logger;
        private readonly Microsoft.AspNetCore.Identity.UserManager<User> userManager;
        private readonly BusinessOwnerRepository businessOwnerRepo;
        private readonly UserService userService;
        private readonly OrderRepository orderRepository;
        private readonly RiderRepository riderRepository;
        private readonly OrderRiderRepository orderRiderRepository;
        private readonly Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> _roleManager;
        private readonly UserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserRepository _userRepository;

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
              IHttpContextAccessor httpContextAccessor

            )
        {
            userManager = _userManager;
            businessOwnerRepo = _businessOwnerRepo;
            orderRepository = _orderRepository;
            riderRepository = _riderRepository;
            _roleManager = roleManager;
            this.orderRiderRepository = orderRiderRepository;
            _userService = userService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
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


        public async void CreateOrder(Order order)
        {
            orderRepository.Add(order);
            orderRepository.CustomSaveChanges();
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







        public async Task<bool> AssignOrderToRiderAsync(int orderId, string riderId)
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

                order.RiderID = riderId;
                order.State = OrderStateEnum.Pending;
                order.ModifiedBy = businessOwnerId;
                order.ModifiedAt = DateTime.Now;

                orderRepository.Update(order);
                await Task.Run(() => orderRepository.CustomSaveChanges());

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





    }
}