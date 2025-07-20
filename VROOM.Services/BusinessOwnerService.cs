using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Transactions;
using Hangfire;
using Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ViewModels;
using ViewModels.Order;
using ViewModels.Shipment;
using ViewModels.User;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Repositories;
using VROOM.ViewModels;
using static NuGet.Packaging.PackagingConstants;
using LocationDto = VROOM.ViewModels.LocationDto;
using RiderVM = VROOM.ViewModels.RiderVM;

namespace VROOM.Services
{
       public class BusinessOwnerService
    {
        private readonly UserManager<User> _userManager;
        private readonly BusinessOwnerRepository _businessOwnerRepo;
        private readonly OrderRepository _orderRepository;
        private readonly RiderRepository _riderRepository;
        private readonly RouteRepository _routeRepository;
        private readonly ShipmentRepository _shipmentRepository;
        private readonly ShipmentServices _shipmentServices;
        private readonly OrderRiderRepository _orderRiderRepository;
        private readonly OrderRouteRepository _orderRouteRepository;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserService _userService;
        private readonly OrderService _orderService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserRepository _userRepository;
        private readonly IHubContext<RiderHub> _hubContext;
        private readonly IHubContext<OwnerHub> _ownerContext;
        private readonly ConcurrentDictionary<string, ShipmentConfirmation> _confirmationStore;
        private readonly CustomerRepository _customerRepository;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly JobRecordService _jobRecordService;
        private readonly ILogger<BusinessOwnerService> _logger;

        public BusinessOwnerService(
            UserManager<User> userManager,
            BusinessOwnerRepository businessOwnerRepo,
            OrderRepository orderRepository,
            RiderRepository riderRepository,
            RouteRepository routeRepository,
            ShipmentRepository shipmentRepository,
            ShipmentServices shipmentServices,
            OrderRiderRepository orderRiderRepository,
            OrderRouteRepository orderRouteRepository,
            RoleManager<IdentityRole> roleManager,
            UserService userService,
            OrderService orderService,
            IHttpContextAccessor httpContextAccessor,
            UserRepository userRepository,
            IHubContext<RiderHub> hubContext,
            IHubContext<OwnerHub> ownerContext,
            ConcurrentDictionary<string, ShipmentConfirmation> confirmationStore,
            CustomerRepository customerRepository,
            IServiceScopeFactory serviceScopeFactory,
            JobRecordService jobRecordService,
            ILogger<BusinessOwnerService> logger)
        {
            _userManager = userManager;
            _businessOwnerRepo = businessOwnerRepo;
            _orderRepository = orderRepository;
            _riderRepository = riderRepository;
            _routeRepository = routeRepository;
            _shipmentRepository = shipmentRepository;
            _shipmentServices = shipmentServices;
            _orderRiderRepository = orderRiderRepository;
            _orderRouteRepository = orderRouteRepository;
            _roleManager = roleManager;
            _userService = userService;
            _orderService = orderService;
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
            _hubContext = hubContext;
            _ownerContext = ownerContext;
            _confirmationStore = confirmationStore;
            _customerRepository = customerRepository;
            _serviceScopeFactory = serviceScopeFactory;
            _jobRecordService = jobRecordService;
            _logger = logger;
        }

        public async Task<bool> UpdateBusinessRegistration(BusinessOwner updatedBusinessInfo)
        {
            var existingBusiness = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == updatedBusinessInfo.UserID);
            if (existingBusiness == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(updatedBusinessInfo.BankAccount))
            {
                existingBusiness.BankAccount = updatedBusinessInfo.BankAccount;
            }
            if (!string.IsNullOrEmpty(updatedBusinessInfo.BusinessType))
            {
                existingBusiness.BusinessType = updatedBusinessInfo.BusinessType;
            }

            _businessOwnerRepo.Update(existingBusiness);
            await _businessOwnerRepo.CustomSaveChangesAsync();
            return true;
        }

        public async Task<BusinessOwnerViewModel> GetBusinessDetails(string businessOwnerId)
        {
            var businessOwner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == businessOwnerId, true, b => b.User);
            if (businessOwner == null)
            {
                return null;
            }

            return new BusinessOwnerViewModel
            {
                UserID = businessOwner.UserID,
                Name = businessOwner.User.Name,
                Email = businessOwner.User.Email,
                BankAccount = businessOwner.BankAccount,
                BusinessType = businessOwner.BusinessType
            };
        }

        private string NormalizePhoneNumber(string phoneNumber)
        {
            phoneNumber = Regex.Replace(phoneNumber ?? "", "[^0-9]", "");
            if (!phoneNumber.StartsWith("+"))
            {
                if (phoneNumber.StartsWith("0"))
                {
                    phoneNumber = $"+20{phoneNumber.Substring(1)}";
                }
                else
                {
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
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("A user with this email already exists: {Email}", request.Email);
                    return Result<RiderVM>.Failure("A user with this email already exists.");
                }

                var businessOwner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == BusinessID);
                if (businessOwner == null)
                {
                    _logger.LogWarning($"Assign failed: No BusinessOwner found with ID {BusinessID}");
                    return Result<RiderVM>.Failure("Business owner not found.");
                }

                var user = new User
                {
                    UserName = request.Email,
                    Email = request.Email,
                    Name = request.Name,
                    PhoneNumber = NormalizePhoneNumber(request.phoneNumber),
                    ProfilePicture = request.ProfilePicture
                };

                _logger.LogInformation("Attempting to create user: {Email}", request.Email);
                var creationResult = await _userManager.CreateAsync(user, request.Password);
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
                await _userManager.AddToRoleAsync(user, role);

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
                _riderRepository.Add(rider);
                await _riderRepository.CustomSaveChangesAsync();

                var result = new RiderVM
                {
                    UserID = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    BusinessID = rider.BusinessID,
                    VehicleType = rider.VehicleType,
                    VehicleStatus = rider.VehicleStatus ?? VehicleTypeStatus.Unknowen,
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
                var user = await _userManager.FindByIdAsync(riderUserId);
                if (user == null)
                {
                    _logger.LogWarning("No user found with ID: {UserId}", riderUserId);
                    return Result<RiderVM>.Failure("User not found.");
                }

                var businessOwner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == BusinessID);
                if (businessOwner == null)
                {
                    _logger.LogWarning($"Update failed: No BusinessOwner found with ID {BusinessID}");
                    return Result<RiderVM>.Failure("Business owner not found.");
                }

                if (!string.IsNullOrEmpty(request.Email) && user.Email != request.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(request.Email);
                    if (existingUser != null)
                    {
                        _logger.LogWarning("A user with this email already exists: {Email}", request.Email);
                        return Result<RiderVM>.Failure("A user with this email already exists.");
                    }
                    user.Email = request.Email;
                    user.UserName = request.Email;
                }

                if (!string.IsNullOrEmpty(request.Name))
                    user.Name = request.Name;
                if (!string.IsNullOrEmpty(request.phoneNumber))
                    user.PhoneNumber = request.phoneNumber;
                if (!string.IsNullOrEmpty(request.ProfilePicture))
                    user.ProfilePicture = request.ProfilePicture;

                _logger.LogInformation("Attempting to update user: {Email}", user.Email);
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errorMessages = string.Join(",", updateResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to update user: {Email}. Errors: {Errors}", user.Email, errorMessages);
                    return Result<RiderVM>.Failure(errorMessages);
                }

                var rider = await _riderRepository.GetLocalOrDbAsync(r => r.UserID == riderUserId);
                if (rider == null)
                {
                    _logger.LogWarning("No rider found for user ID: {UserId}", riderUserId);
                    return Result<RiderVM>.Failure("Rider not found.");
                }

                if (!string.IsNullOrEmpty(BusinessID))
                    rider.BusinessID = BusinessID;
                if (request.VehicleType != default)
                    rider.VehicleType = request.VehicleType;
                if (!string.IsNullOrEmpty(request.VehicleStatus.ToString()))
                    rider.VehicleStatus = request.VehicleStatus;
                if (request.ExperienceLevel != default)
                    rider.ExperienceLevel = request.ExperienceLevel;
                if (request.Location != null)
                {
                    if (request.Location.lat != default)
                        rider.Lat = request.Location.lat;
                    if (request.Location.lang != default)
                        rider.Lang = request.Location.lang;
                    if (!string.IsNullOrEmpty(request.Location.area))
                        rider.Area = request.Location.area;
                }

                _logger.LogInformation("Updating rider in repository for user: {Email}", user.Email);
                _riderRepository.Update(rider);
                await _riderRepository.CustomSaveChangesAsync();

                var result = new RiderVM
                {
                    UserID = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    BusinessID = rider.BusinessID,
                    VehicleType = rider.VehicleType,
                    VehicleStatus = rider.VehicleStatus ?? VehicleTypeStatus.Unknowen,
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

        public async Task<Result<BusinessOwnerProfileVM>> GetProfileAsync(string businessOwnerId)
        {
            try
            {
                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("GetProfileAsync failed: Business Owner ID is empty or null.");
                    return Result<BusinessOwnerProfileVM>.Failure("Business Owner ID is required.");
                }

                var businessOwner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == businessOwnerId, true, b => b.User);
                if (businessOwner == null)
                {
                    _logger.LogWarning($"GetProfileAsync failed: No Business Owner found with ID {businessOwnerId}.");
                    return Result<BusinessOwnerProfileVM>.Failure("Business Owner not found.");
                }

                var roles = await _userManager.GetRolesAsync(businessOwner.User);
                if (!roles.Contains(RoleConstants.BusinessOwner))
                {
                    _logger.LogWarning($"GetProfileAsync failed: User with ID {businessOwnerId} is not a Business Owner.");
                    return Result<BusinessOwnerProfileVM>.Failure("User is not a Business Owner.");
                }

                var totalRiders = (await _riderRepository.GetListLocalOrDbAsync(r => r.BusinessID == businessOwnerId && !r.User.IsDeleted, true, r => r.User)).Count();
                var totalOrders = await _orderRepository.GetList(o => o.Rider != null && o.Rider.BusinessID == businessOwnerId && !o.IsDeleted).CountAsync();

                var profile = new BusinessOwnerProfileVM
                {
                    UserID = businessOwner.UserID,
                    Name = businessOwner.User.Name,
                    Email = businessOwner.User.Email,
                    PhoneNumber = businessOwner.User.PhoneNumber,
                    ProfilePicture = businessOwner.User.ProfilePicture,
                    BankAccount = businessOwner.BankAccount,
                    BusinessType = businessOwner.BusinessType,
                    BusinessLocation = businessOwner.User.Address != null ? new BusinessLocationDto
                    {
                        Latitude = businessOwner.User.Address.Lat,
                        Longitude = businessOwner.User.Address.Lang,
                        AreaName = businessOwner.User.Address.Area
                    } : null,
                    TotalRiders = totalRiders,
                    TotalOrders = totalOrders
                };

                _logger.LogInformation($"Profile retrieved successfully for Business Owner with ID {businessOwnerId}.");
                return Result<BusinessOwnerProfileVM>.Success(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while retrieving profile for Business Owner with ID {businessOwnerId}.");
                return Result<BusinessOwnerProfileVM>.Failure("Error occurred while retrieving profile.");
            }
        }

        public async Task<Result<string>> UpdateProfileAsync(string businessOwnerId, BusinessOwnerProfileVM model)
        {
            try
            {
                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("UpdateProfileAsync failed: Business Owner ID is empty or null.");
                    return Result<string>.Failure("Business Owner ID is required.");
                }

                if (model == null)
                {
                    _logger.LogWarning("UpdateProfileAsync failed: Model data is empty.");
                    return Result<string>.Failure("Profile data is required.");
                }

                var businessOwner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == businessOwnerId, false, b => b.User);
                if (businessOwner == null)
                {
                    _logger.LogWarning($"UpdateProfileAsync failed: No Business Owner found with ID {businessOwnerId}.");
                    return Result<string>.Failure("Business Owner not found.");
                }

                var roles = await _userManager.GetRolesAsync(businessOwner.User);
                if (!roles.Contains(RoleConstants.BusinessOwner))
                {
                    _logger.LogWarning($"UpdateProfileAsync failed: User with ID {businessOwnerId} is not a Business Owner.");
                    return Result<string>.Failure("User is not a Business Owner.");
                }

                var user = businessOwner.User;

                if (!string.IsNullOrEmpty(model.Name))
                    user.Name = model.Name;

                if (!string.IsNullOrEmpty(model.Email))
                {
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        _logger.LogWarning($"UpdateProfileAsync failed: Email {model.Email} is already in use.");
                        return Result<string>.Failure("Email is already in use.");
                    }
                    user.Email = model.Email;
                    user.UserName = model.Email;
                }

                if (!string.IsNullOrEmpty(model.PhoneNumber))
                    user.PhoneNumber = model.PhoneNumber;

                if (model.ProfilePictureFile != null)
                {
                    var filePath = await SaveProfilePictureAsync(model.ProfilePictureFile, businessOwnerId);
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        user.ProfilePicture = filePath;
                    }
                    else
                    {
                        _logger.LogWarning($"UpdateProfileAsync: Failed to save profile picture for user {businessOwnerId}.");
                        return Result<string>.Failure("Failed to save profile picture.");
                    }
                }
                else if (!string.IsNullOrEmpty(model.ProfilePicture))
                {
                    user.ProfilePicture = model.ProfilePicture;
                }

                if (model.BusinessLocation != null)
                {
                    if (user.Address == null)
                        user.Address = new Address { UserID = user.Id };

                    user.Address.Lat = model.BusinessLocation.Latitude;
                    user.Address.Lang = model.BusinessLocation.Longitude;
                    user.Address.Area = model.BusinessLocation.AreaName;
                    _userRepository.Update(user);
                    await _userRepository.CustomSaveChangesAsync();
                }

                var userUpdateResult = await _userManager.UpdateAsync(user);
                if (!userUpdateResult.Succeeded)
                {
                    var errors = string.Join(", ", userUpdateResult.Errors.Select(e => e.Description));
                    _logger.LogError($"UpdateProfileAsync failed: Errors during user update for {businessOwnerId}: {errors}");
                    return Result<string>.Failure(errors);
                }

                if (!string.IsNullOrEmpty(model.BankAccount))
                    businessOwner.BankAccount = model.BankAccount;

                if (!string.IsNullOrEmpty(model.BusinessType))
                    businessOwner.BusinessType = model.BusinessType;

                _businessOwnerRepo.Update(businessOwner);
                await _businessOwnerRepo.CustomSaveChangesAsync();

                _logger.LogInformation($"Profile updated successfully for Business Owner with ID {businessOwnerId}.");
                return Result<string>.Success("Profile updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while updating profile for Business Owner with ID {businessOwnerId}.");
                return Result<string>.Failure($"Error occurred while updating profile: {ex.Message}");
            }
        }

        private async Task<string> SaveProfilePictureAsync(IFormFile file, string businessOwnerId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning($"SaveProfilePictureAsync: Invalid or empty file for user {businessOwnerId}.");
                    return null;
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                var fileName = $"{businessOwnerId}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = $"/uploads/profiles/{fileName}";
                _logger.LogInformation($"Profile picture saved successfully for user {businessOwnerId} at {relativePath}.");
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save profile picture for user {businessOwnerId}.");
                return null;
            }
        }

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
                    var businessOwnerId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(businessOwnerId))
                    {
                        _logger.LogWarning("Failed to create customer: Business Owner ID not found in token.");
                        return Result<CustomerVM>.Failure("Business Owner ID not found in token.");
                    }

                    var businessOwner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == businessOwnerId);
                    if (businessOwner == null)
                    {
                        _logger.LogWarning("Failed to create customer: Business Owner with ID {BusinessOwnerId} not found.", businessOwnerId);
                        return Result<CustomerVM>.Failure("Business Owner not found.");
                    }

                    var roles = await _userManager.GetRolesAsync(businessOwner.User);
                    if (!roles.Contains(RoleConstants.BusinessOwner))
                    {
                        _logger.LogWarning("Failed to create customer: Caller with ID {BusinessOwnerId} is not a Business Owner.", businessOwnerId);
                        return Result<CustomerVM>.Failure("Caller is not a Business Owner.");
                    }

                    var existingUser = await _userManager.FindByEmailAsync(request.Email);
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
                        PhoneNumber = NormalizePhoneNumber(request.PhoneNumber),
                        ProfilePicture = request.ProfilePicture,
                        Address = new Address
                        {
                            Lat = request.Location?.Lat ?? 0,
                            Lang = request.Location?.Lang ?? 0,
                            Area = request.Location?.Area ?? "N/A"
                        }
                    };

                    _logger.LogInformation("Attempting to create user: {Email}", request.Email);
                    var creationResult = await _userManager.CreateAsync(user, request.Password);
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
                    await _userManager.AddToRoleAsync(user, role);

                    user.Address.UserID = user.Id;

                    var customer = new Customer
                    {
                        UserID = user.Id,
                        User = user
                    };

                    _logger.LogInformation("Adding customer to the repository for user: {Email}", request.Email);
                    _customerRepository.Add(customer);
                    await _customerRepository.CustomSaveChangesAsync();

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
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
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

            var creationResult = await _userManager.CreateAsync(user, request.Password);
            if (!creationResult.Succeeded)
            {
                return Result<BusinessOwnerViewModel>.Failure(string.Join(",", creationResult.Errors.Select(e => e.Description)));
            }

            var role = RoleConstants.BusinessOwner;
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            await _userManager.AddToRoleAsync(user, role);

            var businessOwner = new BusinessOwner
            {
                UserID = user.Id,
                User = user,
                BankAccount = request.BankAccount,
                BusinessType = request.BusinessType
            };

            _businessOwnerRepo.Add(businessOwner);
            await _businessOwnerRepo.CustomSaveChangesAsync();

            var businessowner = new BusinessOwnerViewModel
            {
                UserID = user.Id,
                Name = user.Name,
                Email = user.Email,
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

            var rider = await _riderRepository.GetLocalOrDbAsync(r => r.UserID == riderId);
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
                var businessOwnerId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("Assign failed: BusinessOwner ID not found in context.");
                    return false;
                }

                var businessOwner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == businessOwnerId);
                if (businessOwner == null)
                {
                    _logger.LogWarning($"Assign failed: No BusinessOwner found with ID {businessOwnerId}");
                    return false;
                }

                var order = await _orderRepository.GetLocalOrDbAsync(o => o.Id == orderId && !o.IsDeleted);
                if (order == null)
                {
                    _logger.LogWarning($"Assign failed: Order ID {orderId} not found or deleted.");
                    return false;
                }

                var orderRoute = await _orderRouteRepository.GetLocalOrDbAsync(or => or.OrderID == orderId);
                if (orderRoute == null)
                {
                    _logger.LogWarning($"Assign failed: Route for order {orderId} not found.");
                    return false;
                }

                var route = await _routeRepository.GetLocalOrDbAsync(r => r.Id == orderRoute.RouteID);
                if (route == null)
                {
                    _logger.LogWarning($"Assign failed: Route {orderRoute.RouteID} not found.");
                    return false;
                }

                var shipment = await _shipmentRepository.GetLocalOrDbAsync(
                    sh => !sh.IsDeleted && sh.waypoints.Any(w => w.orderId == orderId),
                    false,
                    sh => sh.Routes,
                    sh => sh.waypoints
                );

                if (shipment == null)
                {
                    shipment = await _shipmentServices.CreateShipment(new AddShipmentVM
                    {
                        startTime = route.Start,
                        InTransiteBeginTime = DateTime.Now.Add(order.PrepareTime),
                        BeginningLang = route.OriginLang,
                        BeginningLat = route.OriginLat,
                        BeginningArea = route.OriginArea,
                        EndLang = route.DestinationLang,
                        EndLat = route.DestinationLat,
                        EndArea = route.DestinationArea,
                        zone = order.zone,
                        MaxConsecutiveDeliveries = 10,
                        OrderIds = [order.Id]
                    });

                    route.ShipmentID = shipment.Id;
                    _routeRepository.Update(route);
                    _shipmentRepository.Update(shipment);
                    await _shipmentRepository.CustomSaveChangesAsync();
                    await _routeRepository.CustomSaveChangesAsync();
                }

                if (order.State != OrderStateEnum.Created && order.State != OrderStateEnum.Pending)
                {
                    _logger.LogWarning($"Assign failed: Order ID {order.Id} is in invalid state: {order.State}");
                    return false;
                }

                var rider = await _riderRepository.GetLocalOrDbAsync(r => r.UserID == riderId && !r.User.IsDeleted, true, r => r.User);
                if (rider == null || rider.BusinessID != businessOwner.UserID || rider.Status != RiderStatusEnum.Available)
                {
                    _logger.LogWarning($"Assign failed: Rider ID {riderId} is invalid or unavailable.");
                    return false;
                }

                order.RiderID = riderId;
                order.State = OrderStateEnum.Pending;
                order.ModifiedBy = businessOwnerId;
                order.ModifiedAt = DateTime.Now;
                _orderRepository.Update(order);
                await _orderRepository.CustomSaveChangesAsync();

                var notificationSent = await NotifyRiderWithRetry(riderId, shipment.Id, new List<int> { orderId }, businessOwnerId, maxRetries: 2, retryDelay: TimeSpan.FromSeconds(5));
                if (!notificationSent)
                {
                    _logger.LogWarning($"Failed to notify rider {riderId} for shipment {shipment.Id} after retries.");
                    order.RiderID = null;
                    order.State = OrderStateEnum.Created;
                    _orderRepository.Update(order);
                    await _orderRepository.CustomSaveChangesAsync();
                    return false;
                }

                var confirmation = await WaitForRiderShipmentResponseAsync(riderId, shipment.Id, timeoutSeconds: 30);
                if (confirmation != ConfirmationStatus.Accepted)
                {
                    string message = confirmation == ConfirmationStatus.Rejected
                        ? $"Rider {rider.User.Name} rejected shipment {shipment.Id}."
                        : $"Rider {rider.User.Name} did not respond to shipment {shipment.Id}.";
                    _logger.LogInformation(message);
                    await _ownerContext.Clients.User(businessOwnerId).SendAsync("ReceiveNotification", message);

                    order.RiderID = null;
                    order.State = OrderStateEnum.Created;
                    _orderRepository.Update(order);
                    await _orderRepository.CustomSaveChangesAsync();
                    return false;
                }

                string successMessage = $"Rider {rider.User.Name} accepted shipment {shipment.Id}.";
                _logger.LogInformation(successMessage);
                await _ownerContext.Clients.User(businessOwnerId).SendAsync("ReceiveNotification", successMessage);

                order.State = OrderStateEnum.Confirmed;
                _orderRepository.Update(order);

                shipment.ShipmentState = ShipmentStateEnum.Assigned;
                shipment.RiderID = riderId;
                _shipmentRepository.Update(shipment);
                await _shipmentRepository.CustomSaveChangesAsync();
                await _orderRepository.CustomSaveChangesAsync();

                rider.Status = RiderStatusEnum.OnDelivery;
                _riderRepository.Update(rider);
                await _riderRepository.CustomSaveChangesAsync();

                _logger.LogInformation($"Shipment {shipment.Id} successfully assigned to Rider {riderId} by BusinessOwner {businessOwnerId}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while assigning shipment for order {orderId} to Rider {riderId}.");
                return false;
            }
        }

        public async Task<OrderDetailsViewModel?> ViewAssignedOrderAsync(int orderId)
        {
            try
            {
                var riderId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(riderId))
                {
                    _logger.LogWarning("View failed: Rider ID not found in context.");
                    return null;
                }

                var rider = await _riderRepository.GetLocalOrDbAsync(r => r.UserID == riderId, true, r => r.User);
                if (rider == null || rider.Status != RiderStatusEnum.Available)
                {
                    _logger.LogWarning($"View failed: Rider ID {riderId} not found or not available.");
                    return null;
                }

                var order = await _orderRepository.GetLocalOrDbAsync(o => o.Id == orderId && !o.IsDeleted, true, o => o.Rider, o => o.Customer, o => o.Rider.BusinessOwner);
                if (order == null)
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
        public async Task<Result<RiderDashboardResult>> GetRiders()
        {
            try
            {
                // Extract Business Owner ID from the HTTP context
                var businessOwnerId = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("Failed to retrieve riders: Business Owner ID not found in token.");
                    return Result<RiderDashboardResult>.Failure("Business Owner ID not found in token.");
                }

                // Verify Business Owner exists
                var businessOwner = await _businessOwnerRepo.GetAsync(businessOwnerId);
                if (businessOwner == null)
                {
                    _logger.LogWarning("Failed to retrieve riders: Business Owner with ID {BusinessOwnerId} not found.", businessOwnerId);
                    return Result<RiderDashboardResult>.Failure("Business Owner not found.");
                }

                // Verify the caller is a Business Owner
                var roles = await _userManager.GetRolesAsync(businessOwner.User);
                if (!roles.Contains(RoleConstants.BusinessOwner))
                {
                    _logger.LogWarning("Failed to retrieve riders: Caller with ID {BusinessOwnerId} is not a Business Owner.", businessOwnerId);
                    return Result<RiderDashboardResult>.Failure("Caller is not a Business Owner.");
                }

                // Fetch riders associated with the Business Owner
                var riders = await _riderRepository.GetRidersForBusinessOwnerAsync(businessOwnerId);

                int onDeliveryCount = riders.Count(r => r.Status == RiderStatusEnum.OnDelivery);

                // Count riders with Available status
                int availableCount = riders.Count(r => r.Status == RiderStatusEnum.Available);

                // Map riders to RiderVM
                var riderVMs = riders.Select(r => new RiderVM
                {
                    UserID = r.UserID,
                    Name = r.User.Name,
                    Email = r.User.Email,
                    phoneNumber = r.User.PhoneNumber,
                    BusinessID = r.BusinessID,
                    VehicleType = r.VehicleType,
                    VehicleStatus = r.VehicleStatus ?? VehicleTypeStatus.Unknowen,
                    ExperienceLevel = r.ExperienceLevel,
                    Location = new LocationDto
                    {
                        Lat = r.Lat,
                        Lang = r.Lang,
                        Area = r.Area
                    },
                    Status = r.Status,
                    ProfilePicture = r.User.ProfilePicture
                }).ToList();

                _logger.LogInformation("Successfully retrieved {RiderCount} riders for Business Owner with ID {BusinessOwnerId}.", riderVMs.Count, businessOwnerId);
                return Result<RiderDashboardResult>.Success(new RiderDashboardResult { riderVMs = riderVMs, OnDeliveryCount = onDeliveryCount, AvailableCount = availableCount });
            }
            catch (Exception ex)
            {
                //  _logger.LogError(ex, "An error occurred while retrieving riders for Business Owner with ID {BusinessOwnerId}.", businessOwnerId);
                return Result<RiderDashboardResult>.Failure("An error occurred while retrieving riders.");
            }
        }

        public async Task<Result<List<CustomerVM>>> GetCustomers()
        {
            try
            {
                var businessOwnerId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("Failed to retrieve customers: Business Owner ID not found in token.");
                    return Result<List<CustomerVM>>.Failure("Business Owner ID not found in token.");
                }

                var businessOwner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == businessOwnerId);
                if (businessOwner == null)
                {
                    _logger.LogWarning("Failed to retrieve customers: Business Owner with ID {BusinessOwnerId} not found.", businessOwnerId);
                    return Result<List<CustomerVM>>.Failure("Business Owner not found.");
                }

                var roles = await _userManager.GetRolesAsync(businessOwner.User);
                if (!roles.Contains(RoleConstants.BusinessOwner))
                {
                    _logger.LogWarning("Failed to retrieve customers: Caller with ID {BusinessOwnerId} is not a Business Owner.", businessOwnerId);
                    return Result<List<CustomerVM>>.Failure("Caller is not a Business Owner.");
                }

                var customers = await _userRepository.GetCustomersByBusinessOwnerIdAsync(businessOwnerId);
                var customerVMs = new List<CustomerVM>();
                foreach (var customer in customers)
                {
                    var customerRoles = await _userManager.GetRolesAsync(customer);
                    if (customerRoles.Contains(RoleConstants.Customer))
                    {
                        customerVMs.Add(new CustomerVM
                        {
                            UserID = customer.Id,
                            Name = customer.Name,
                            Email = customer.Email,
                            PhoneNumber = customer.PhoneNumber,
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
                var businessOwnerId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("Failed to retrieve all customers: Business Owner ID not found in token.");
                    return Result<List<CustomerVM>>.Failure("Business Owner ID not found in token.");
                }

                var businessOwner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == businessOwnerId);
                if (businessOwner == null)
                {
                    _logger.LogWarning("Failed to retrieve all customers: Business Owner with ID {BusinessOwnerId} not found.", businessOwnerId);
                    return Result<List<CustomerVM>>.Failure("Business Owner not found.");
                }

                var roles = await _userManager.GetRolesAsync(businessOwner.User);
                if (!roles.Contains(RoleConstants.BusinessOwner))
                {
                    _logger.LogWarning("Failed to retrieve all customers: Caller with ID {BusinessOwnerId} is not a Business Owner.", businessOwnerId);
                    return Result<List<CustomerVM>>.Failure("Caller is not a Business Owner.");
                }

                var customers = await _customerRepository.GetListLocalOrDbAsync(null, true, c => c.User);
                var customerVMs = new List<CustomerVM>();
                foreach (var customer in customers)
                {
                    var user = await _userManager.FindByIdAsync(customer.UserID);
                    if (user != null)
                    {
                        var customerRoles = await _userManager.GetRolesAsync(user);
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

        public async Task<bool> PrepareOrder(OrderCreateViewModel _orderCreateVM)
        {
            try
            {
                var businessOwnerId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("Prepare order failed: BusinessOwner ID not found in context.");
                    return false;
                }

                var businessOwner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == businessOwnerId);
                if (businessOwner == null)
                {
                    _logger.LogWarning($"Prepare order failed: No BusinessOwner found with ID {businessOwnerId}.");
                    return false;
                }

                var order = await _orderService.CreateOrder(_orderCreateVM, businessOwnerId);
                var orderRoute = await _orderRouteRepository.GetLocalOrDbAsync(or => or.OrderID == order.Id);
                var route = await _routeRepository.GetLocalOrDbAsync(r => r.Id == orderRoute.RouteID);

                if (orderRoute == null || route == null)
                {
                    _logger.LogWarning($"Route or OrderRoute not found for order {order.Id}.");
                    return false;
                }

                TimeSpan setWaitingTime;
                if (order.PrepareTime == null)
                {
                    _logger.LogWarning($"PrepareTime is null for order {order.Id}. Using default preparation time.");
                    setWaitingTime = TimeSpan.FromMinutes(10);
                }
                else if (order.OrderPriority == OrderPriorityEnum.HighUrgent)
                {
                    setWaitingTime = order.PrepareTime;
                }
                else if (order.OrderPriority == OrderPriorityEnum.Urgent)
                {
                    setWaitingTime = order.PrepareTime + TimeSpan.FromMinutes(1);
                }
                else
                {
                    setWaitingTime = order.PrepareTime + TimeSpan.FromMinutes(10);
                }

                var prepareTime = order.PrepareTime;
                var minInTransiteTime = DateTime.Now.Add(prepareTime);
                var highPriorityMinTime = DateTime.Now.Add(prepareTime + TimeSpan.FromMinutes(5));

                var shipment = await _shipmentRepository.GetLocalOrDbAsync(
                    sh => !sh.IsDeleted &&
                          (sh.Routes == null || sh.Routes.Count < sh.MaxConsecutiveDeliveries) &&
                          (sh.ShipmentState == ShipmentStateEnum.Created || sh.ShipmentState == ShipmentStateEnum.Assigned) &&
                          sh.zone == order.zone &&
                          sh.InTransiteBeginTime > minInTransiteTime &&
                          (order.OrderPriority != OrderPriorityEnum.HighUrgent || sh.InTransiteBeginTime >= highPriorityMinTime),
                    true,
                    sh => sh.Routes,
                    sh => sh.waypoints
                );

                if (shipment != null)
                {
                    route.ShipmentID = shipment.Id;
                    _routeRepository.Update(route);
                    order.State = OrderStateEnum.Pending;
                    _orderRepository.Update(order);
                    await _orderRepository.CustomSaveChangesAsync();

                    var lastRoute = shipment.Routes?.OrderByDescending(r => r.DestinationLang)
                        .ThenByDescending(r => r.DestinationLat).FirstOrDefault();

                    if (lastRoute != null)
                    {
                        double lastLat = lastRoute.DestinationLat;
                        double lastLng = lastRoute.DestinationLang;
                        double newLat = route.DestinationLat;
                        double newLng = route.DestinationLang;

                        double distance = Math.Sqrt(Math.Pow(newLat - lastLat, 2) + Math.Pow(newLng - lastLng, 2));
                        double threshold = 0.01;

                        shipment.waypoints = shipment.waypoints ?? new List<Waypoint>();

                        if (distance > threshold)
                        {
                            shipment.waypoints.Add(new Waypoint
                            {
                                ShipmentID = shipment.Id,
                                Lang = shipment.EndLang,
                                Lat = shipment.EndLat,
                                Area = shipment.EndArea,
                                orderId = order.Id
                            });
                            shipment.EndLat = newLat;
                            shipment.EndLang = newLng;
                            shipment.EndArea = route.DestinationArea;
                        }
                        else
                        {
                            shipment.waypoints.Add(new Waypoint
                            {
                                ShipmentID = shipment.Id,
                                Lang = route.DestinationLang,
                                Lat = route.DestinationLat,
                                Area = route.DestinationArea,
                                orderId = order.Id
                            });
                        }
                    }
                    else
                    {
                        shipment.waypoints = shipment.waypoints ?? new List<Waypoint>();
                        shipment.waypoints.Add(new Waypoint
                        {
                            ShipmentID = shipment.Id,
                            Lang = route.DestinationLang,
                            Lat = route.DestinationLat,
                            Area = route.DestinationArea,
                            orderId = order.Id
                        });
                    }

                    _shipmentRepository.Update(shipment);
                    await _shipmentRepository.CustomSaveChangesAsync();
                    await _routeRepository.CustomSaveChangesAsync();

                    if (order.OrderPriority == OrderPriorityEnum.HighUrgent)
                    {
                        var result = await AssignOrderAutomaticallyAsync(businessOwnerId, shipment);
                        if (!result.IsSuccess)
                        {
                            _logger.LogWarning($"Failed to assign high-priority shipment {shipment.Id}: {result.Error}");
                            return false;
                        }
                        return true;
                    }
                    else
                    {
                        var jobId = $"AssignShipment_{shipment.Id}";
                        var jobExists = await _jobRecordService.CheckIfJobExistsAsync(shipment.Id);
                        if (!jobExists)
                        {
                            var hangfireJobId = BackgroundJob.Schedule(
                                () => AssignOrderAutomaticallyJobAsync(businessOwnerId, shipment.Id, jobId),
                                setWaitingTime);
                            await _jobRecordService.AddJobRecordAsync(jobId, shipment.Id, hangfireJobId);
                        }

                        return true;
                    }
                }
                else
                {
                    shipment = await _shipmentServices.CreateShipment(new AddShipmentVM
                    {
                        startTime = route.Start,
                        InTransiteBeginTime = DateTime.Now.Add(setWaitingTime),
                        BeginningLang = route.OriginLang,
                        BeginningLat = route.OriginLat,
                        BeginningArea = route.OriginArea,
                        EndLang = route.DestinationLang,
                        EndLat = route.DestinationLat,
                        EndArea = route.DestinationArea,
                        zone = order.zone,
                        MaxConsecutiveDeliveries = 10,
                        OrderIds = [order.Id]
                    });

                    route.ShipmentID = shipment.Id;
                    _routeRepository.Update(route);
                    order.State = OrderStateEnum.Pending;
                    _orderRepository.Update(order);
                    await _orderRepository.CustomSaveChangesAsync();
                    _shipmentRepository.Update(shipment);
                    await _shipmentRepository.CustomSaveChangesAsync();
                    await _routeRepository.CustomSaveChangesAsync();

                    if (order.OrderPriority == OrderPriorityEnum.HighUrgent)
                    {
                        var result = await AssignOrderAutomaticallyAsync(businessOwnerId, shipment);
                        if (!result.IsSuccess)
                        {
                            _logger.LogWarning($"Failed to assign high-priority shipment {shipment.Id}: {result.Error}");
                            return false;
                        }
                        return true;
                    }
                    else
                    {
                        var jobId = $"AssignShipment_{shipment.Id}";
                        var jobExists = await _jobRecordService.CheckIfJobExistsAsync(shipment.Id);
                        if (!jobExists)
                        {
                            var hangfireJobId = BackgroundJob.Schedule(
                                () => AssignOrderAutomaticallyJobAsync(businessOwnerId, shipment.Id, jobId),
                                setWaitingTime);
                            await _jobRecordService.AddJobRecordAsync(jobId, shipment.Id, hangfireJobId);
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while preparing order.");
                return false;
            }
        }

        public async Task<bool> ViewOrder(int orderId, string riderId, bool isAccepted)
        {
            try
            {
                var order = await _orderRepository.GetLocalOrDbAsync(o => o.Id == orderId && !o.IsDeleted);
                if (order == null || order.State != OrderStateEnum.Pending || order.RiderID != riderId)
                {
                    return false;
                }

                var rider = await _riderRepository.GetLocalOrDbAsync(r => r.UserID == riderId && !r.User.IsDeleted);
                if (rider == null || rider.Status != RiderStatusEnum.Available)
                {
                    return false;
                }

                var orderRider = await _orderRiderRepository.GetLocalOrDbAsync(or => or.OrderID == orderId && or.RiderID == riderId && !or.IsDeleted);
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

                    _orderRepository.Update(order);
                    _riderRepository.Update(rider);
                    _orderRiderRepository.Update(orderRider);

                    await _orderRepository.CustomSaveChangesAsync();
                    await _riderRepository.CustomSaveChangesAsync();
                    await _orderRiderRepository.CustomSaveChangesAsync();

                    return true;
                }
                else
                {
                    order.State = OrderStateEnum.Pending;
                    order.ModifiedBy = riderId;
                    order.ModifiedAt = DateTime.Now;

                    rider.Status = RiderStatusEnum.Available;

                    orderRider.IsDeleted = true;
                    orderRider.ModifiedBy = riderId;
                    orderRider.ModifiedAt = DateTime.Now;

                    _orderRepository.Update(order);
                    _riderRepository.Update(rider);
                    _orderRiderRepository.Update(orderRider);

                    await _orderRepository.CustomSaveChangesAsync();
                    await _riderRepository.CustomSaveChangesAsync();
                    await _orderRiderRepository.CustomSaveChangesAsync();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task AssignOrderAutomaticallyJobAsync(string businessOwnerId, int shipmentId, string jobId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var shipmentRepository = scope.ServiceProvider.GetRequiredService<ShipmentRepository>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<BusinessOwnerService>>();

            var shipment = await shipmentRepository.GetLocalOrDbAsync(
                s => s.Id == shipmentId && !s.IsDeleted,
                true,
                s => s.waypoints,
                s => s.Routes
            );

            if (shipment == null)
            {
                logger.LogWarning($"Shipment {shipmentId} not found or deleted for Hangfire job {jobId}.");
                await _jobRecordService.UpdateJobStatusAsync(jobId, shipmentId, "Failed", "Shipment not found or deleted.");
                return;
            }

            var result = await AssignOrderAutomaticallyAsync(businessOwnerId, shipment);
            if (!result.IsSuccess)
            {
                logger.LogWarning($"Hangfire job {jobId} failed to assign shipment {shipmentId}: {result.Error}");
                await _jobRecordService.UpdateJobStatusAsync(jobId, shipmentId, "Failed", result.Error);
            }
            else
            {
                logger.LogInformation($"Hangfire job {jobId} successfully assigned shipment {shipmentId}.");
                await _jobRecordService.UpdateJobStatusAsync(jobId, shipmentId, "Completed");
            }
        }

        public async Task<Result> AssignOrderAutomaticallyAsync(string businessOwnerId, Shipment shipment)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<BusinessOwnerService>>();

            var businessOwner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == businessOwnerId);
            if (businessOwner == null)
            {
                logger.LogWarning($"Business owner with ID {businessOwnerId} not found.");
                return Result.Failure("Business owner not found.");
            }
            var orderIds_Weights = shipment.waypoints?.Select(w => new { id = w.orderId, weight = w.Order.Weight }).ToList();
            var orderIds = orderIds_Weights.Select(o => o.id).ToList();

            var orders = await _orderRepository.GetListLocalOrDbAsync(o => orderIds.Contains(o.Id) && !o.IsDeleted, false);

            try
            {
                shipment = await _shipmentRepository.GetLocalOrDbAsync(
                    s => s.Id == shipment.Id && !s.IsDeleted,
                    false,
                    s => s.waypoints,
                    s => s.Routes
                );

                if (shipment == null)
                {
                    logger.LogWarning($"Shipment {shipment.Id} not found or deleted.");
                    return Result.Failure("Shipment not found.");
                }


                if (!orders.Any())
                {
                    logger.LogWarning($"No valid orders found in shipment {shipment.Id}.");
                    return Result.Failure("No valid orders found in shipment.");
                }

                foreach (var order in orders)
                {
                    var orderRoute = await _orderRouteRepository.GetLocalOrDbAsync(or => or.OrderID == order.Id);
                    if (orderRoute == null)
                    {
                        logger.LogWarning($"Route for order {order.Id} not found.");
                        return Result.Failure($"Route for order {order.Id} not found.");
                    }

                    var route = await _routeRepository.GetLocalOrDbAsync(r => r.Id == orderRoute.RouteID);
                    if (route == null)
                    {
                        logger.LogWarning($"Route with ID {orderRoute.RouteID} not found.");
                        return Result.Failure($"Route {orderRoute.RouteID} not found.");
                    }
                }

                int maxCycles = 3;
                int currentCycle = 0;
                var attemptedRiders = new HashSet<string>();
                var rejectedRiders = new HashSet<string>();
                TimeSpan delayBetweenCycles = TimeSpan.FromSeconds(10);

                while (currentCycle < maxCycles)
                {
                    orderIds_Weights = shipment.waypoints?.Select(w => new { id = w.orderId, weight = w.Order.Weight }).ToList();
                    orders = await _orderRepository.GetListLocalOrDbAsync(o => orderIds_Weights.Select(o => o.id).Contains(o.Id) && !o.IsDeleted, true);
                    var maxWeights = _orderRepository.GetMaxWeight(shipment.Id);

                    var orderHasMaxWeight = (await _orderRepository.GetListLocalOrDbAsync(o => o.Weight == maxWeights)).FirstOrDefault();
                    var riders = await _riderRepository.GetAvaliableRiders(businessOwnerId);
                    var filteredRiders = riders
                        .Where(r => r.VehicleStatus == VehicleTypeStatus.Good || r.VehicleStatus == VehicleTypeStatus.Excellant && IsVehicleSuitable(r.VehicleType, orderHasMaxWeight))
                        .ToList();

                    if (!filteredRiders.Any())
                    {
                        foreach (var order in orders)
                        {
                            order.RiderID = null;
                            order.State = OrderStateEnum.Created;
                            order.ModifiedBy = businessOwnerId;
                            order.ModifiedAt = DateTime.Now;
                            _orderRepository.Update(order);
                        }
                        await _orderRepository.CustomSaveChangesAsync();
                        logger.LogWarning($"No suitable riders for shipment {shipment.Id} in cycle {currentCycle + 1}.");
                        return Result.Failure("No suitable riders found for this shipment.");
                    }

                    var firstOrder = orders.First();
                    var firstOrderRoute = await _orderRouteRepository.GetLocalOrDbAsync(or => or.OrderID == firstOrder.Id);
                    var firstRoute = await _routeRepository.GetLocalOrDbAsync(r => r.Id == firstOrderRoute.RouteID);

                    var scoredRiders = filteredRiders
                        .Select(r =>
                        {
                            var distance = Haversine(firstRoute.OriginLat, firstRoute.OriginLang, r.Lat, r.Lang);
                            var scoreDistance = CalculateDistanceScore(distance, filteredRiders, firstRoute.OriginLat, firstRoute.OriginLang) * 0.5;
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

                        logger.LogInformation($"Attempting to assign shipment {shipment.Id} to rider {rider.UserID} (Cycle {currentCycle + 1}/{maxCycles}, Score: {scoredRider.TotalScore:F2}).");

                        foreach (var order in orders)
                        {
                            order.RiderID = rider.UserID;
                            order.ModifiedBy = businessOwnerId;
                            order.ModifiedAt = DateTime.Now;
                            _orderRepository.Update(order);
                        }
                        await _orderRepository.CustomSaveChangesAsync();

                        var notificationSent = await NotifyRiderWithRetry(rider.UserID, shipment.Id, orderIds, businessOwnerId, maxRetries: 2, retryDelay: TimeSpan.FromSeconds(5));
                        if (!notificationSent)
                        {
                            logger.LogWarning($"Failed to notify rider {rider.UserID} for shipment {shipment.Id} after retries.");
                            rejectedRiders.Add(rider.UserID);
                            continue;
                        }

                        var confirmation = await WaitForRiderShipmentResponseAsync(rider.UserID, shipment.Id, timeoutSeconds: 20);
                        if (confirmation == ConfirmationStatus.Accepted)
                        {
                            foreach (var order in orders)
                            {
                                order.State = OrderStateEnum.Confirmed;
                                _orderRepository.Update(order);
                            }

                            shipment.ShipmentState = ShipmentStateEnum.Assigned;
                            shipment.RiderID = rider.UserID;
                            _shipmentRepository.Update(shipment);
                            await _shipmentRepository.CustomSaveChangesAsync();
                            await _orderRepository.CustomSaveChangesAsync();

                            rider.Status = RiderStatusEnum.OnDelivery;
                            _riderRepository.Update(rider);
                            await _riderRepository.CustomSaveChangesAsync();

                            logger.LogInformation($"Shipment {shipment.Id} assigned to rider {rider.UserID} successfully.");
                            await NotifyRiderConfirmationAsync(rider.UserID, shipment.Id, true, "Shipment assigned successfully.");
                            return Result.Success("Shipment assigned successfully.");
                        }
                        else
                        {
                            logger.LogInformation($"Rider {rider.UserID} {(confirmation == ConfirmationStatus.Rejected ? "rejected" : "did not respond to")} shipment {shipment.Id}.");
                            rejectedRiders.Add(rider.UserID);
                            await NotifyRiderConfirmationAsync(rider.UserID, shipment.Id, false, confirmation == ConfirmationStatus.Rejected ? "Shipment rejected." : "Response timed out.");
                            continue;
                        }
                    }

                    attemptedRiders.Clear();
                    currentCycle++;
                    if (currentCycle < maxCycles)
                    {
                        logger.LogInformation($"No rider accepted shipment {shipment.Id} in cycle {currentCycle}. Waiting {delayBetweenCycles.TotalSeconds} seconds.");
                        await Task.Delay(delayBetweenCycles);
                    }
                }

                foreach (var order in orders)
                {
                    order.RiderID = null;
                    order.State = OrderStateEnum.Created;
                    order.ModifiedBy = businessOwnerId;
                    order.ModifiedAt = DateTime.Now;
                    _orderRepository.Update(order);
                }
                await _orderRepository.CustomSaveChangesAsync();
                logger.LogWarning($"Failed to assign shipment {shipment.Id} after {maxCycles} cycles.");
                return Result.Failure("No rider accepted the shipment after maximum attempts.");
            }
            catch (Exception ex)
            {
                foreach (var order in orders)
                {
                    order.RiderID = null;
                    order.State = OrderStateEnum.Created;
                    order.ModifiedBy = businessOwnerId;
                    order.ModifiedAt = DateTime.Now;
                    _orderRepository.Update(order);
                }
                logger.LogError(ex, $"Error assigning shipment {shipment.Id} for business owner {businessOwnerId}.");
                return Result.Failure("An error occurred while assigning the shipment.");
            }
        }

        private async Task<ConfirmationStatus> WaitForRiderShipmentResponseAsync(string riderId, int shipmentId, int timeoutSeconds)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(timeoutSeconds))
            {
                if (_confirmationStore.TryGetValue(riderId, out var confirmation) &&
                    confirmation.ShipmentId == shipmentId &&
                    confirmation.Status != ConfirmationStatus.Pending)
                {
                    return confirmation.Status;
                }
                await Task.Delay(1000);
            }
            return ConfirmationStatus.Pending;
        }

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

        private async Task<bool> NotifyRiderWithRetry(string riderId, int shipmentId, List<int> orderIds, string businessOwnerId, int maxRetries, TimeSpan retryDelay)
        {
            int attempt = 0;
            while (attempt <= maxRetries)
            {
                try
                {
                    await NotifyRiderForShipmentConfirmation(riderId, shipmentId, orderIds, businessOwnerId);
                    _logger.LogInformation($"Notification sent to rider {riderId} for shipment {shipmentId} on attempt {attempt + 1}.");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to notify rider {riderId} for shipment {shipmentId} on attempt {attempt + 1}.");
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

        private async Task NotifyRiderConfirmationAsync(string riderId, int shipmentId, bool success, string message)
        {
            try
            {
                await _hubContext.Clients.User(riderId).SendAsync("ShipmentResponseConfirmation", new
                {
                    ShipmentId = shipmentId,
                    Success = success,
                    Message = message
                });
                _logger.LogInformation($"Confirmation sent to rider {riderId} for shipment {shipmentId}: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send confirmation to rider {riderId} for shipment {shipmentId}.");
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
            var owner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == userId);
            if (owner == null) throw new Exception("Owner not found");

            owner.SubscriptionType = SubscriptionTypeEnum.Trial;
            owner.SubscriptionStartDate = DateTime.Now;
            owner.SubscriptionEndDate = DateTime.Now.AddDays(7);

            _businessOwnerRepo.Update(owner);
            await _businessOwnerRepo.CustomSaveChangesAsync();
        }

        public async Task ActivatePaidAsync(string userId)
        {
            var owner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == userId);
            if (owner == null) throw new Exception("Owner not found");

            owner.SubscriptionType = SubscriptionTypeEnum.Paid;
            owner.SubscriptionStartDate = DateTime.Now;
            owner.SubscriptionEndDate = DateTime.Now.AddMonths(1);

            _businessOwnerRepo.Update(owner);
            await _businessOwnerRepo.CustomSaveChangesAsync();
        }

        public async Task RenewSubscriptionAsync(string userId)
        {
            var owner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == userId);
            if (owner == null) throw new Exception("Owner not found");

            if (owner.SubscriptionEndDate.HasValue && owner.SubscriptionEndDate > DateTime.Now)
                owner.SubscriptionEndDate = owner.SubscriptionEndDate.Value.AddMonths(1);
            else
                owner.SubscriptionEndDate = DateTime.Now.AddMonths(1);

            owner.SubscriptionType = SubscriptionTypeEnum.Paid;

            _businessOwnerRepo.Update(owner);
            await _businessOwnerRepo.CustomSaveChangesAsync();
        }

        private async Task NotifyRiderForShipmentConfirmation(string riderId, int shipmentId, List<int> orderIds, string businessOwnerId)
        {
            try
            {
                var orders = await _orderRepository.GetListLocalOrDbAsync(o => orderIds.Contains(o.Id) && !o.IsDeleted, true);
                if (!orders.Any())
                {
                    _logger.LogWarning($"Notification failed: No orders found for shipment {shipmentId}.");
                    return;
                }

                var firstOrder = orders.First();
                var orderRoute = await _orderRouteRepository.GetLocalOrDbAsync(or => or.OrderID == firstOrder.Id);
                if (orderRoute == null)
                {
                    _logger.LogWarning($"Notification failed: Route for order {firstOrder.Id} not found.");
                    return;
                }

                var route = await _routeRepository.GetLocalOrDbAsync(r => r.Id == orderRoute.RouteID);
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
                ShipmentDto shipment = await _shipmentServices.GetShipmentByIdAsync(shipmentId);
                var shipmentData = new
                {
                    ShipmentId = shipmentId,
                    OrderTitles = orders.Select(o => $"Order #{o.Title}").ToList(),
                    OrderDetails = orders.Select(o => o.Details).ToList(),
                    Message = $"You have a new shipment with {orders?.Count()} orders. Please confirm within 30 seconds.",
                    Expiry = message.ExpiryTime.ToString("o"),
                    From = new
                    {
                        Area = route.OriginArea,
                        Lat = route.OriginLat,
                        Lng = route.OriginLang
                    },
                    To = new
                    {
                        Area = ((ZoneEnum)int.Parse(route.DestinationArea)).ToString(),
                        Lat = route.DestinationLat,
                        Lng = route.DestinationLang
                    },
                    PickupTime = shipment.InTransiteBeginTime.ToString() ?? DateTime.UtcNow.ToString("o"),
                    NumOfOrders = shipment.Waypoints.Count(),
                    OrderPriority = firstOrder.OrderPriority.ToString() ?? "Normal",
                    RiderId = riderId
                };

                await _hubContext.Clients.User(riderId).SendAsync("ReceiveShipmentRequest", shipmentData);
                _logger.LogInformation($"Notification sent to rider {riderId} for shipment {shipmentId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to notify rider {riderId} for shipment {shipmentId}.");
                throw;
            }
        }

        public async Task<Result<string>> CreateOrderAndAssignAsync(CreateOrderWithAssignmentRequest request)
        {
            var businessOwnerId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            try
            {
                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning("Failed to create order: Business Owner ID not found in token.");
                    return Result<string>.Failure("Business Owner ID not found in token.");
                }

                var businessOwner = await _businessOwnerRepo.GetLocalOrDbAsync(b => b.UserID == businessOwnerId);
                if (businessOwner == null)
                {
                    _logger.LogWarning("Failed to create order: Business Owner with ID {BusinessOwnerId} not found.", businessOwnerId);
                    return Result<string>.Failure("Business Owner not found.");
                }

                var roles = await _userManager.GetRolesAsync(businessOwner.User);
                if (!roles.Contains(RoleConstants.BusinessOwner))
                {
                    _logger.LogWarning("Failed to create order: Caller with ID {BusinessOwnerId} is not a Business Owner.", businessOwnerId);
                    return Result<string>.Failure("Caller is not a Business Owner.");
                }

                bool assignmentSuccess = false;
                if (request.AssignmentType?.ToLower() == "manual" && !string.IsNullOrEmpty(request.RiderId))
                {
                    var order = await _orderService.CreateOrder(request.Order, businessOwnerId);
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
                        _logger.LogWarning("Automatic assignment failed for order.");
                        return Result<string>.Failure("Error in automatic assignment.");
                    }
                    assignmentSuccess = true;
                }
                else
                {
                    _logger.LogWarning("Invalid or missing assignment type for order.");
                    return Result<string>.Failure("Invalid or missing assignment type. Use 'Manual' or 'Automatic'.");
                }

                return Result<string>.Success("Order created and assigned successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create and assign order for Business Owner {businessOwnerId}.");
                return Result<string>.Failure($"An error occurred while creating and assigning the order: {ex.Message}");
            }
        }

        public async Task CheckAndAssignOverdueShipments()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var currentTime = DateTime.Now.AddMinutes(-2);

            var overdueShipments = await _shipmentRepository.GetListLocalOrDbAsync(
                s => s.InTransiteBeginTime.HasValue && s.InTransiteBeginTime.Value <= currentTime && s.ShipmentState == ShipmentStateEnum.Created && !s.IsDeleted,
                false,
                s => s.waypoints,
                s => s.Routes);

            foreach (var shipment in overdueShipments)
            {
                var orderIds_Weights = shipment.waypoints?.Select(w => new { id = w.orderId, weight = w.Order.Weight }).ToList();
                if (orderIds_Weights == null || !orderIds_Weights.Any())
                {
                    _logger.LogWarning($"No waypoints found for shipment {shipment.Id}. Skipping.");
                    continue;
                }

                var orderIds = orderIds_Weights.Select(o => o.id).ToList();
                var orders = await _orderRepository.GetListLocalOrDbAsync(o => orderIds.Contains(o.Id) && !o.IsDeleted && o.State == OrderStateEnum.Created, true);
                if (!orders.Any())
                {
                    _logger.LogWarning($"No valid orders found for shipment {shipment.Id}. Skipping.");
                    continue;
                }

                var maxWeight = orderIds_Weights.Max(o => o.weight);
                var orderHasMaxWeight = orders.FirstOrDefault(o => o.Weight == maxWeight);
                if (orderHasMaxWeight == null)
                {
                    _logger.LogWarning($"No order with max weight found for shipment {shipment.Id}. Skipping.");
                    continue;
                }

                var businessOwnerId = shipment.waypoints.FirstOrDefault()?.Order?.BusinessID;
                if (string.IsNullOrEmpty(businessOwnerId))
                {
                    _logger.LogWarning($"No BusinessOwner ID found for shipment {shipment.Id}. Skipping.");
                    continue;
                }

                var result = await AssignOrderAutomaticallyAsync(businessOwnerId, shipment);
                if (result.IsSuccess)
                {
                    _logger.LogInformation($"Shipment {shipment.Id} assigned automatically.");
                    continue;
                }

                _logger.LogWarning($"Automatic assignment failed for shipment {shipment.Id}: {result.Error}. Attempting forced assignment.");

                var availableRiders = await _riderRepository.GetListLocalOrDbAsync(r => r.BusinessID == businessOwnerId && !r.User.IsDeleted && r.Status == RiderStatusEnum.Available, true, r => r.User);
                var suitableRiders = availableRiders.Where(r => r.VehicleStatus == VehicleTypeStatus.Good || r.VehicleStatus == VehicleTypeStatus.Excellant && IsVehicleSuitable(r.VehicleType, orderHasMaxWeight))
                        .ToList();

                if (suitableRiders.Any())
                {
                    var firstOrder = orders.First();
                    var firstOrderRoute = await _orderRouteRepository.GetLocalOrDbAsync(or => or.OrderID == firstOrder.Id);
                    if (firstOrderRoute == null)
                    {
                        _logger.LogWarning($"Route for order {firstOrder.Id} not found. Skipping forced assignment.");
                        continue;
                    }

                    var firstRoute = await _routeRepository.GetLocalOrDbAsync(r => r.Id == firstOrderRoute.RouteID);
                    if (firstRoute == null)
                    {
                        _logger.LogWarning($"Route {firstOrderRoute.RouteID} not found. Skipping forced assignment.");
                        continue;
                    }

                    var scoredRiders = suitableRiders
                        .Select(r =>
                        {
                            var distance = Haversine(firstRoute.OriginLat, firstRoute.OriginLang, r.Lat, r.Lang);
                            var scoreDistance = CalculateDistanceScore(distance, suitableRiders, firstRoute.OriginLat, firstRoute.OriginLang) * 0.5;
                            var scoreExperience = GetExperienceScore(r.ExperienceLevel) * 0.2;
                            var scoreRating = r.Rating * 20 * 0.3;
                            var totalScore = scoreDistance + scoreExperience + scoreRating;
                            return new { Rider = r, TotalScore = totalScore, Distance = distance };
                        })
                        .OrderByDescending(x => x.TotalScore)
                        .ToList();

                    var bestRider = scoredRiders.First();
                    _logger.LogWarning($"Forced assignment for shipment {shipment.Id}. Assigning best rider {bestRider.Rider.UserID} due to passed delivery time (Score: {bestRider.TotalScore:F2}).");

                    foreach (var order in orders)
                    {
                        order.RiderID = bestRider.Rider.UserID;
                        order.State = OrderStateEnum.Confirmed;
                        order.ModifiedBy = businessOwnerId;
                        order.ModifiedAt = DateTime.Now;
                        _orderRepository.Update(order);
                    }

                    shipment.ShipmentState = ShipmentStateEnum.Assigned;
                    shipment.RiderID = bestRider.Rider.UserID;
                    _shipmentRepository.Update(shipment);

                    bestRider.Rider.Status = RiderStatusEnum.OnDelivery;
                    _riderRepository.Update(bestRider.Rider);

                    await _orderRepository.CustomSaveChangesAsync();
                    await _shipmentRepository.CustomSaveChangesAsync();
                    await _riderRepository.CustomSaveChangesAsync();

                    await NotifyRiderConfirmationAsync(bestRider.Rider.UserID, shipment.Id, true, "Shipment assigned due to passed delivery time.");
                    _logger.LogInformation($"Shipment {shipment.Id} forcefully assigned to rider {bestRider.Rider.UserID} successfully.");
                }
                else
                {
                    _logger.LogWarning($"No available riders for forced assignment of shipment {shipment.Id}. Notifying owner.");
                    await _ownerContext.Clients.User(businessOwnerId).SendAsync("ReceiveNotification", $"No available riders for forced assignment of shipment {shipment.Id}.");
                }
            }
        }

        public async Task CheckOrderCreatedWithoutShipments()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var currentTime = DateTime.Now;

            var orderCreatedWithoutShipment = await _orderRepository.GetListLocalOrDbAsync(
                o => o.State == OrderStateEnum.Created && !o.IsDeleted && o.OrderRoute != null && o.OrderRoute.Route != null &&
                     (o.OrderRoute.Route.ShipmentID == null || o.OrderRoute.Route.Shipment.ShipmentState == ShipmentStateEnum.InTransit || o.OrderRoute.Route.Shipment.ShipmentState == ShipmentStateEnum.Delivered),
                true,
                o => o.OrderRoute,
                o => o.OrderRoute.Route,
                o => o.OrderRoute.Route.Shipment);

            if (!orderCreatedWithoutShipment.Any())
            {
                _logger.LogInformation("No orders in Created state or with InTransit/Delivered shipments.");
                return;
            }

            var ordersByShipment = orderCreatedWithoutShipment.GroupBy(o => o.OrderRoute.Route.ShipmentID ?? 0).ToList();

            foreach (var ordersGroup in ordersByShipment)
            {
                var shipmentId = ordersGroup.Key;
                var orders = ordersGroup.ToList();

                var firstOrder = orders.OrderBy(o => o.Date).First();
                var firstOrderRoute = firstOrder.OrderRoute.Route;

                var lastOrder = orders.OrderBy(o => o.Date).Last();
                var lastOrderRoute = lastOrder.OrderRoute.Route;

                var shipment = shipmentId != 0 ? await _shipmentRepository.GetLocalOrDbAsync(s => s.Id == shipmentId, true, s => s.waypoints) : null;

                DateTime? inTransiteBegin;
                if (shipment != null && shipment.InTransiteBeginTime.HasValue)
                {
                    inTransiteBegin = shipment.InTransiteBeginTime.Value;
                }
                else
                {
                    inTransiteBegin = firstOrder.Date;
                }

                TimeSpan additionalTime;
                if (firstOrder.PrepareTime == null)
                {
                    _logger.LogWarning($"PrepareTime is null for order {firstOrder.Id}. Using default preparation time.");
                    additionalTime = TimeSpan.FromMinutes(10);
                }
                else if (firstOrder.OrderPriority == OrderPriorityEnum.HighUrgent)
                {
                    additionalTime = firstOrder.PrepareTime;
                }
                else if (firstOrder.OrderPriority == OrderPriorityEnum.Urgent)
                {
                    additionalTime = firstOrder.PrepareTime + TimeSpan.FromMinutes(3);
                }
                else
                {
                    additionalTime = firstOrder.PrepareTime + TimeSpan.FromMinutes(10);
                }

                inTransiteBegin = inTransiteBegin.Value.Add(additionalTime);

                var addShipmentVM = new AddShipmentVM
                {
                    startTime = DateTime.Now,
                    InTransiteBeginTime = inTransiteBegin,
                    OrderIds = orders.Select(o => o.Id).ToList(),
                    BeginningLat = firstOrderRoute.OriginLat,
                    BeginningLang = firstOrderRoute.OriginLang,
                    BeginningArea = firstOrderRoute.OriginArea,
                    EndLat = lastOrderRoute.DestinationLat,
                    EndLang = lastOrderRoute.DestinationLang,
                    EndArea = lastOrderRoute.DestinationArea,
                    zone = firstOrder.zone,
                    RiderID = null,
                    MaxConsecutiveDeliveries = 10
                };

                var newShipment = await _shipmentServices.CreateShipment(addShipmentVM);
                if (newShipment == null)
                {
                    _logger.LogWarning($"Failed to create new shipment for orders {string.Join(", ", orders.Select(o => o.Id))}.");
                    continue;
                }

                foreach (var order in orders)
                {
                    var orderRoute = await _orderRouteRepository.GetLocalOrDbAsync(or => or.OrderID == order.Id);
                    if (orderRoute != null)
                    {
                        var route = await _routeRepository.GetLocalOrDbAsync(r => r.Id == orderRoute.RouteID);
                        if (route != null)
                        {
                            route.ShipmentID = newShipment.Id;
                            _routeRepository.Update(route);
                        }
                    }
                }

                await _routeRepository.CustomSaveChangesAsync();
                _logger.LogInformation($"New shipment {newShipment.Id} created for orders {string.Join(", ", orders.Select(o => o.Id))}.");
            }
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(string ownerUserId)
        {
            if (string.IsNullOrEmpty(ownerUserId))
            {
                _logger.LogWarning("GetDashboardStatsAsync failed: Owner UserID is empty.");
                throw new ArgumentException("Owner UserID cannot be empty.");
            }

            try
            {
                var stats = await _businessOwnerRepo.GetDashboardStatsAsync(ownerUserId);
                if (stats == null)
                {
                    _logger.LogWarning($"No dashboard stats found for Business Owner with ID {ownerUserId}.");
                    throw new KeyNotFoundException("No dashboard stats found.");
                }

                _logger.LogInformation($"Dashboard stats retrieved successfully for Business Owner with ID {ownerUserId}.");
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching dashboard statistics for Business Owner with ID {ownerUserId}.");
                throw new Exception("Error fetching dashboard statistics.", ex);
            }
        }
    }

}