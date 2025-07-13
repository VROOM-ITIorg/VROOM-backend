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
using Hangfire;
using Hangfire.Server;
using Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Protocol.Core.Types;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
//using ViewModels.Business_Owner;
using ViewModels.Shipment;
using ViewModels.User;
using VROOM.Data;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Repositories;
using VROOM.Repository;
using VROOM.ViewModels;
using ViewModels.Order;
namespace VROOM.Services;



public class CreateOrderWithAssignmentRequest
{
    public OrderCreateViewModel Order { get; set; }
    public string AssignmentType { get; set; } // "Manual" or "Automatic"
    public string? RiderId { get; set; } // Required for manual assignment
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
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly JobRecordService jobRecordService;
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
        CustomerRepository _customerRepository,
        IServiceScopeFactory serviceScopeFactory,
        JobRecordService _jobRecordService
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
        _serviceScopeFactory = serviceScopeFactory;
        jobRecordService = _jobRecordService;
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


    //public BusinessOwnerViewModel GetBusinessDetails(string businessOwnerId)
    //{
    //    var businessOwner = businessOwnerRepo.GetAsync(businessOwnerId).Result;

    //    return new BusinessOwnerViewModel
    //    {
    //        UserID = businessOwner.UserID,
    //        BankAccount = businessOwner.BankAccount,
    //        BusinessType = businessOwner.BusinessType
    //    };
    //}


    //private async Task SendWhatsAppMessage(string phoneNumber, string userMessage)
    //{
    //    string formatedPhoneNumber = NormalizePhoneNumber(phoneNumber);
    //    var client = new HttpClient();
    //    client.DefaultRequestHeaders.Add("Authorization", "Bearer ed2a766dcd8fdc7ba0dcb7958b263f03727be139e06a2ad294973eaf04d0a69f6bf58f4b4c810c93");
    //    var payload = new
    //    {
    //        phone = formatedPhoneNumber,
    //        message = userMessage
    //    };

    //    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    //    var response = await client.PostAsync("https://api.wassenger.com/v1/messages", content);
    //    response.EnsureSuccessStatusCode();


    //    _logger.LogInformation(response.Content.ToString());

    //}

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
                //PhoneNumber = request.phoneNumber,
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
            //await SendWhatsAppMessage(user.PhoneNumber, $"Greating, You are a rider for {businessOwner.User.Name} Business now, try to login with your username: {rider.User.UserName} and password : {request.Password} , You are his slave now congrates!😊");

            var result = new RiderVM
            {
                UserID = user.Id,
                Name = user.Name,
                Email = user.Email,
                //phoneNumber = user.PhoneNumber,
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
     //Profile
    public async Task<Result<BusinessOwnerProfileVM>> GetProfileAsync(string businessOwnerId)
    {
        try
        {
            if (string.IsNullOrEmpty(businessOwnerId))
            {
                _logger.LogWarning("GetProfileAsync فشلت: معرف صاحب الأعمال فارغ أو null.");
                return Result<BusinessOwnerProfileVM>.Failure("معرف صاحب الأعمال مطلوب.");
            }

            var businessOwner = await businessOwnerRepo.GetAsync(businessOwnerId);
            if (businessOwner == null)
            {
                _logger.LogWarning($"GetProfileAsync فشلت: لم يتم العثور على صاحب أعمال بمعرف {businessOwnerId}.");
                return Result<BusinessOwnerProfileVM>.Failure("صاحب الأعمال غير موجود.");
            }

            var roles = await userManager.GetRolesAsync(businessOwner.User);
            if (!roles.Contains(RoleConstants.BusinessOwner))
            {
                _logger.LogWarning($"GetProfileAsync فشلت: المستخدم بمعرف {businessOwnerId} ليس صاحب أعمال.");
                return Result<BusinessOwnerProfileVM>.Failure("المستخدم ليس صاحب أعمال.");
            }

            // حساب عدد السائقين وعدد الطلبات
            // حساب عدد السائقين وعدد الطلبات
            var totalRiders = (await riderRepository.GetRidersForBusinessOwnerAsync(businessOwnerId)).Count();
            var totalOrders = await orderRepository.GetList(o => o.Rider != null && o.Rider.BusinessID == businessOwnerId && !o.IsDeleted).CountAsync();

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

            _logger.LogInformation($"تم جلب الملف الشخصي بنجاح لصاحب الأعمال بمعرف {businessOwnerId}.");
            return Result<BusinessOwnerProfileVM>.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"حدث خطأ أثناء جلب الملف الشخصي لصاحب الأعمال بمعرف {businessOwnerId}.");
            return Result<BusinessOwnerProfileVM>.Failure("حدث خطأ أثناء جلب الملف الشخصي.");
        }
    }
    public async Task<Result<string>> UpdateProfileAsync(string businessOwnerId, BusinessOwnerProfileVM model)
    {
        try
        {
            if (string.IsNullOrEmpty(businessOwnerId))
            {
                _logger.LogWarning("UpdateProfileAsync فشلت: معرف صاحب الأعمال فارغ أو null.");
                return Result<string>.Failure("معرف صاحب الأعمال مطلوب.");
            }

            if (model == null)
            {
                _logger.LogWarning("UpdateProfileAsync فشلت: نموذج البيانات فارغ.");
                return Result<string>.Failure("بيانات الملف الشخصي مطلوبة.");
            }

            var businessOwner = await businessOwnerRepo.GetAsync(businessOwnerId);
            if (businessOwner == null)
            {
                _logger.LogWarning($"UpdateProfileAsync فشلت: لم يتم العثور على صاحب أعمال بمعرف {businessOwnerId}.");
                return Result<string>.Failure("صاحب الأعمال غير موجود.");
            }

            var roles = await userManager.GetRolesAsync(businessOwner.User);
            if (!roles.Contains(RoleConstants.BusinessOwner))
            {
                _logger.LogWarning($"UpdateProfileAsync فشلت: المستخدم بمعرف {businessOwnerId} ليس صاحب أعمال.");
                return Result<string>.Failure("المستخدم ليس صاحب أعمال.");
            }

            var user = businessOwner.User;

            // تحديث بيانات المستخدم
            if (!string.IsNullOrEmpty(model.Name))
                user.Name = model.Name;

            if (!string.IsNullOrEmpty(model.Email))
            {
                var existingUser = await userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    _logger.LogWarning($"UpdateProfileAsync فشلت: البريد الإلكتروني {model.Email} مستخدم بالفعل.");
                    return Result<string>.Failure("البريد الإلكتروني مستخدم بالفعل.");
                }
                user.Email = model.Email;
                user.UserName = model.Email;
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber))
                user.PhoneNumber = model.PhoneNumber;

            // معالجة تحميل الصورة الشخصية
            if (model.ProfilePictureFile != null)
            {
                var filePath = await SaveProfilePictureAsync(model.ProfilePictureFile, businessOwnerId);
                if (!string.IsNullOrEmpty(filePath))
                {
                    user.ProfilePicture = filePath;
                }
                else
                {
                    _logger.LogWarning($"UpdateProfileAsync: فشل في حفظ الصورة الشخصية للمستخدم {businessOwnerId}.");
                    return Result<string>.Failure("فشل في حفظ الصورة الشخصية.");
                }
            }
            else if (!string.IsNullOrEmpty(model.ProfilePicture))
            {
                user.ProfilePicture = model.ProfilePicture;
            }

            // تحديث بيانات موقع الأعمال
            if (model.BusinessLocation != null)
            {
                if (user.Address == null)
                    user.Address = new Address { UserID = user.Id };

                user.Address.Lat = model.BusinessLocation.Latitude;
                user.Address.Lang = model.BusinessLocation.Longitude;
                user.Address.Area = model.BusinessLocation.AreaName;
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
            }

            // تحديث المستخدم في قاعدة البيانات
            var userUpdateResult = await userManager.UpdateAsync(user);
            if (!userUpdateResult.Succeeded)
            {
                var errors = string.Join(", ", userUpdateResult.Errors.Select(e => e.Description));
                _logger.LogError($"UpdateProfileAsync فشلت: أخطاء أثناء تحديث المستخدم {businessOwnerId}: {errors}");
                return Result<string>.Failure(errors);
            }

            // تحديث بيانات صاحب الأعمال
            if (!string.IsNullOrEmpty(model.BankAccount))
                businessOwner.BankAccount = model.BankAccount;

            if (!string.IsNullOrEmpty(model.BusinessType))
                businessOwner.BankAccount = model.BankAccount;

            businessOwnerRepo.Update(businessOwner);
            businessOwnerRepo.CustomSaveChanges();

            _logger.LogInformation($"تم تحديث الملف الشخصي بنجاح لصاحب الأعمال بمعرف {businessOwnerId}.");
            return Result<string>.Success("تم تحديث الملف الشخصي بنجاح.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"حدث خطأ أثناء تحديث الملف الشخصي لصاحب الأعمال بمعرف {businessOwnerId}.");
            return Result<string>.Failure($"حدث خطأ أثناء تحديث الملف الشخصي: {ex.Message}");
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

    private async Task<string> SaveProfilePictureAsync(IFormFile file, string businessOwnerId)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning($"SaveProfilePictureAsync: ملف غير صالح أو فارغ للمستخدم {businessOwnerId}.");
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
            _logger.LogInformation($"تم حفظ الصورة الشخصية بنجاح للمستخدم {businessOwnerId} في {relativePath}.");
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"فشل في حفظ الصورة الشخصية للمستخدم {businessOwnerId}.");
            return null;
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
            var businessOwnerId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

            var orderRoute = orderRouteRepository.GetOrderRouteByOrderID(orderId);
            if (orderRoute == null)
            {
                _logger.LogWarning($"Assign failed: Route for order {orderId} not found.");
                return false;
            }

            var route = await routeRepository.GetAsync(orderRoute.RouteID);
            if (route == null)
            {
                _logger.LogWarning($"Assign failed: Route {orderRoute.RouteID} not found.");
                return false;
            }

            var shipment = await shipmentRepository
                .GetList(sh => !sh.IsDeleted && sh.waypoints.Any(w => w.orderId == orderId))
                .Include(s => s.Routes)
                .Include(s => s.waypoints)
                .FirstOrDefaultAsync();

            if (shipment == null)
            {

                shipment = await shipmentServices.CreateShipment(new AddShipmentVM
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
                routeRepository.Update(route);
                shipmentRepository.Update(shipment);
                shipmentRepository.CustomSaveChanges();
                routeRepository.CustomSaveChanges();

            }

            //var orderIds = shipment.waypoints?.Select(w => w.orderId).ToList() ?? new List<int>();

            if (order.State != OrderStateEnum.Created && order.State != OrderStateEnum.Pending)
            {
                _logger.LogWarning($"Assign failed: Order ID {order.Id} is in invalid state: {order.State}");
                return false;
            }


            var rider = await riderRepository.GetAsync(riderId);
            if (rider == null || rider.BusinessID != businessOwner.UserID || rider.Status != RiderStatusEnum.Available)
            {
                _logger.LogWarning($"Assign failed: Rider ID {riderId} is invalid or unavailable.");
                return false;
            }

            // Temporarily assign all orders in the shipment to the rider

            order.RiderID = riderId;
            order.State = OrderStateEnum.Pending;
            order.ModifiedBy = businessOwnerId;
            order.ModifiedAt = DateTime.Now;
            orderRepository.Update(order);

            orderRepository.CustomSaveChanges();

            // Notify rider and wait for response
            var notificationSent = await NotifyRiderWithRetry(riderId, shipment.Id, [orderId], businessOwnerId, maxRetries: 2, retryDelay: TimeSpan.FromSeconds(5));
            if (!notificationSent)
            {
                _logger.LogWarning($"Failed to notify rider {riderId} for shipment {shipment.Id} after retries.");
                // Reset order states

                order.RiderID = null;
                order.State = OrderStateEnum.Created;
                orderRepository.Update(order);

                orderRepository.CustomSaveChanges();
                return false;
            }

            // Wait for rider response (accept/reject)
            var confirmation = await WaitForRiderShipmentResponseAsync(riderId, shipment.Id, timeoutSeconds: 30);
            if (confirmation != ConfirmationStatus.Accepted)
            {
                string message = confirmation == ConfirmationStatus.Rejected
                    ? $"Rider {rider.User.Name} rejected shipment {shipment.Id}."
                    : $"Rider {rider.User.Name} did not respond to shipment {shipment.Id}.";
                _logger.LogInformation(message);

                // Notify business owner via OwnerHub
                await ownerContext.Clients.User(businessOwnerId).SendAsync("ReceiveNotification", message);

                // Reset order states

                order.RiderID = null;
                order.State = OrderStateEnum.Created;
                orderRepository.Update(order);

                orderRepository.CustomSaveChanges();
                return false;
            }

            // Rider accepted the shipment
            string successMessage = $"Rider {rider.User.Name} accepted shipment {shipment.Id}.";
            _logger.LogInformation(successMessage);
            await ownerContext.Clients.User(businessOwnerId).SendAsync("ReceiveNotification", successMessage);

            // Update all orders to Confirmed

            order.State = OrderStateEnum.Confirmed;
            orderRepository.Update(order);


            // Update shipment state
            shipment.ShipmentState = ShipmentStateEnum.Assigned;
            shipment.RiderID = riderId;
            shipmentRepository.Update(shipment);
            shipmentRepository.CustomSaveChanges();
            orderRepository.CustomSaveChanges();

            rider.Status = RiderStatusEnum.OnDelivery;
            riderRepository.Update(rider);
            riderRepository.CustomSaveChanges();

            _logger.LogInformation($"Shipment {shipment.Id} successfully assigned to Rider {riderId} by BusinessOwner {businessOwnerId}.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Exception occurred while assigning shipment for order {orderId} to Rider {riderId}.");
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


    public async Task<bool> PrepareOrder(OrderCreateViewModel _orderCreateVM)
    {
        try
        {
            var businessOwnerId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(businessOwnerId))
            {
                _logger.LogWarning("Prepare order failed: BusinessOwner ID not found in context.");
                return false;
            }

            var businessOwner = await businessOwnerRepo.GetAsync(businessOwnerId);
            if (businessOwner == null)
            {
                _logger.LogWarning($"Prepare order failed: No BusinessOwner found with ID {businessOwnerId}.");
                return false;
            }

            var order = await orderService.CreateOrder(_orderCreateVM, businessOwnerId);
            var orderRoute = orderRouteRepository.GetOrderRouteByOrderID(order.Id);
            var route = await routeRepository.GetAsync(orderRoute.RouteID);

            if (orderRoute == null || route == null)
            {
                _logger.LogWarning($"Route or OrderRoute not found for order {order.Id}.");
                return false;
            }

            // تحديد setWaitingTime
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
                setWaitingTime = order.PrepareTime + TimeSpan.FromMinutes(3);
            }
            else
            {
                setWaitingTime = order.PrepareTime + TimeSpan.FromMinutes(10);
            }

            var prepareTime = order.PrepareTime;
            var minInTransiteTime = DateTime.Now.Add(prepareTime);
            var highPriorityMinTime = DateTime.Now.Add(prepareTime + TimeSpan.FromMinutes(5));

            var shipment = await shipmentRepository
                .GetList(sh => !sh.IsDeleted &&
                               (sh.Routes == null || sh.Routes.Count < sh.MaxConsecutiveDeliveries) &&
                               (sh.ShipmentState == ShipmentStateEnum.Created || sh.ShipmentState == ShipmentStateEnum.Assigned) &&
                               sh.zone == order.zone &&
                               sh.InTransiteBeginTime > minInTransiteTime &&
                               (order.OrderPriority != OrderPriorityEnum.HighUrgent ||
                                sh.InTransiteBeginTime >= highPriorityMinTime))
                .Include(s => s.Routes)
                .Include(s => s.waypoints)
                .FirstOrDefaultAsync();

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            if (shipment != null)
            {
                // إضافة الأمر إلى الـ Shipment الموجود
                route.ShipmentID = shipment.Id;
                routeRepository.Update(route);
                order.State = OrderStateEnum.Pending;
                orderRepository.Update(order);
                orderRepository.CustomSaveChanges();
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

                shipmentRepository.Update(shipment);
                shipmentRepository.CustomSaveChanges();
                routeRepository.CustomSaveChanges();

                // التعامل حسب الأولوية
                if (order.OrderPriority == OrderPriorityEnum.HighUrgent)
                {
                    var result = await AssignOrderAutomaticallyAsync(businessOwnerId, shipment);
                    if (!result.IsSuccess)
                    {
                        _logger.LogWarning($"Failed to assign high-priority shipment {shipment.Id}: {result.Error}");
                        return false;
                    }
                    transaction.Complete();
                    return true;
                }
                else
                {
                    var jobId = $"AssignShipment_{shipment.Id}";
                    var jobExists = await jobRecordService.CheckIfJobExistsAsync(shipment.Id);
                    if (!jobExists)
                    {
                        var hangfireJobId = BackgroundJob.Schedule(
                            () => AssignOrderAutomaticallyJobAsync(businessOwnerId, shipment.Id, jobId),
                            setWaitingTime);

                        await jobRecordService.AddJobRecordAsync(jobId, shipment.Id, hangfireJobId);

                    }

                    transaction.Complete();
                    return true;
                }
            }
            else
            {

                shipment = await shipmentServices.CreateShipment(new AddShipmentVM
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
                routeRepository.Update(route);
                order.State = OrderStateEnum.Pending;
                orderRepository.Update(order);
                orderRepository.CustomSaveChanges();
                shipmentRepository.Update(shipment);
                shipmentRepository.CustomSaveChanges();
                routeRepository.CustomSaveChanges();


                if (order.OrderPriority == OrderPriorityEnum.HighUrgent)
                {
                    var result = await AssignOrderAutomaticallyAsync(businessOwnerId, shipment);
                    if (!result.IsSuccess)
                    {
                        _logger.LogWarning($"Failed to assign high-priority shipment {shipment.Id}: {result.Error}");
                        return false;
                    }
                    transaction.Complete();
                    return true;
                }
                else
                {
                    var jobId = $"AssignShipment_{shipment.Id}";
                    var jobExists = await jobRecordService.CheckIfJobExistsAsync(shipment.Id);
                    if (!jobExists)
                    {
                        var hangfireJobId = BackgroundJob.Schedule(
                            () => AssignOrderAutomaticallyJobAsync(businessOwnerId, shipment.Id, jobId),
                            setWaitingTime);

                        await jobRecordService.AddJobRecordAsync(jobId, shipment.Id, hangfireJobId);
                    }
                    transaction.Complete();
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

    public async Task AssignOrderAutomaticallyJobAsync(string businessOwnerId, int shipmentId, string jobId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var shipmentRepository = scope.ServiceProvider.GetRequiredService<ShipmentRepository>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BusinessOwnerService>>();

        var shipment = await shipmentRepository
            .GetList(s => s.Id == shipmentId && !s.IsDeleted)
            .Include(s => s.waypoints)
            .Include(s => s.Routes)
            .FirstOrDefaultAsync();

        if (shipment == null)
        {
            logger.LogWarning($"Shipment {shipmentId} not found or deleted for Hangfire job {jobId}.");
            await jobRecordService.UpdateJobStatusAsync(jobId, shipmentId, "Failed", "Shipment not found or deleted.");
            return;
        }

        var result = await AssignOrderAutomaticallyAsync(businessOwnerId, shipment);
        if (!result.IsSuccess)
        {
            logger.LogWarning($"Hangfire job {jobId} failed to assign shipment {shipmentId}: {result.Error}");
            await jobRecordService.UpdateJobStatusAsync(jobId, shipmentId, "Failed", result.Error);
        }
        else
        {
            logger.LogInformation($"Hangfire job {jobId} successfully assigned shipment {shipmentId}.");
            await jobRecordService.UpdateJobStatusAsync(jobId, shipmentId, "Completed");
        }
    }


    public async Task<Result> AssignOrderAutomaticallyAsync(string businessOwnerId, Shipment shipment)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BusinessOwnerService>>();

        var businessOwner = await businessOwnerRepo.GetAsync(businessOwnerId);
        if (businessOwner == null)
        {
            logger.LogWarning($"Business owner with ID {businessOwnerId} not found.");
            return Result.Failure("Business owner not found.");
        }

        try
        {
            // تحميل waypoints للـ Shipment
            shipment = await shipmentRepository
                .GetList(s => s.Id == shipment.Id && !s.IsDeleted)
                .Include(s => s.waypoints)
                .Include(s => s.Routes)
                .FirstOrDefaultAsync();

            if (shipment == null)
            {
                logger.LogWarning($"Shipment {shipment.Id} not found or deleted.");
                return Result.Failure("Shipment not found.");
            }


            var orderIds = shipment.waypoints?.Select(w => w.orderId).ToList() ?? new List<int>();
            var orders = orderRepository.GetList(o => orderIds.Contains(o.Id) && !o.IsDeleted);
            if (!orders.Any())
            {
                logger.LogWarning($"No valid orders found in shipment {shipment.Id}.");
                return Result.Failure("No valid orders found in shipment.");
            }


            foreach (var order in orders)
            {
                //if (order.State != OrderStateEnum.Pending)
                //{
                //    logger.LogInformation($"Order {order.Id} is not in Created state. Stopping assignment for shipment {shipment.Id}.");
                //    return Result.Failure($"Order {order.Id} is not pending.");
                //}

                var orderRoute = orderRouteRepository.GetOrderRouteByOrderID(order.Id);
                if (orderRoute == null)
                {
                    logger.LogWarning($"Route for order {order.Id} not found.");
                    return Result.Failure($"Route for order {order.Id} not found.");
                }

                var route = await routeRepository.GetAsync(orderRoute.RouteID);
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

                orderIds = shipment.waypoints?.Select(w => w.orderId).ToList() ?? new List<int>();
                orders = orderRepository.GetList(o => orderIds.Contains(o.Id) && !o.IsDeleted);
                if (orders.Any(o => o.State != OrderStateEnum.Pending))
                {
                    logger.LogInformation($"One or more orders in shipment {shipment.Id} are no longer pending. Stopping assignment.");
                    return Result.Failure("One or more orders are no longer pending.");
                }

                var riders = await riderRepository.GetAvaliableRiders(businessOwnerId);
                var filteredRiders = riders
                    .Where(r => r.VehicleStatus == "Good")
                    .ToList();

                if (!filteredRiders.Any())
                {
                    logger.LogWarning($"No suitable riders for shipment {shipment.Id} in cycle {currentCycle + 1}.");
                    return Result.Failure("No suitable riders found for this shipment.");
                }


                var firstOrder = orders.First();
                var firstOrderRoute = orderRouteRepository.GetOrderRouteByOrderID(firstOrder.Id);
                var firstRoute = await routeRepository.GetAsync(firstOrderRoute.RouteID);

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

                    // تعيين السائق مؤقتًا لكل الأوامر
                    foreach (var order in orders)
                    {
                        order.RiderID = rider.UserID;
                        order.State = OrderStateEnum.Pending;
                        order.ModifiedBy = businessOwnerId;
                        order.ModifiedAt = DateTime.Now;
                        orderRepository.Update(order);
                    }
                    orderRepository.CustomSaveChanges();


                    var notificationSent = await NotifyRiderWithRetry(rider.UserID, shipment.Id, orderIds, businessOwnerId, maxRetries: 2, retryDelay: TimeSpan.FromSeconds(5));
                    if (!notificationSent)
                    {
                        logger.LogWarning($"Failed to notify rider {rider.UserID} for shipment {shipment.Id} after retries.");
                        rejectedRiders.Add(rider.UserID);
                        continue;
                    }

                    var confirmation = await WaitForRiderShipmentResponseAsync(rider.UserID, shipment.Id, timeoutSeconds: 30);
                    if (confirmation == ConfirmationStatus.Accepted)
                    {

                        orders = orderRepository.GetList(o => orderIds.Contains(o.Id));
                        foreach (var order in orders)
                        {
                            order.State = OrderStateEnum.Confirmed;
                            orderRepository.Update(order);
                        }

                        shipment.ShipmentState = ShipmentStateEnum.Assigned;
                        shipment.RiderID = rider.UserID;
                        shipmentRepository.Update(shipment);
                        shipmentRepository.CustomSaveChanges();
                        orderRepository.CustomSaveChanges();

                        rider.Status = RiderStatusEnum.OnDelivery;
                        riderRepository.Update(rider);
                        riderRepository.CustomSaveChanges();

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
                orderRepository.Update(order);
            }
            orderRepository.CustomSaveChanges();
            logger.LogWarning($"Failed to assign shipment {shipment.Id} after {maxCycles} cycles.");
            return Result.Failure("No rider accepted the shipment after maximum attempts.");
        }
        catch (Exception ex)
        {
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

    private async Task NotifyRiderForShipmentConfirmation(string riderId, int shipmentId, List<int> orderIds, string businessOwnerId)
    {
        try
        {
            var orders = orderRepository.GetList(o => orderIds.Contains(o.Id) && !o.IsDeleted);
            if (!orders.Any())
            {
                _logger.LogWarning($"Notification failed: No orders found for shipment {shipmentId}.");
                return;
            }

            var firstOrder = orders.First();
            var orderRoute = orderRouteRepository.GetOrderRouteByOrderID(firstOrder.Id);
            if (orderRoute == null)
            {
                _logger.LogWarning($"Notification failed: Route for order {firstOrder.Id} not found.");
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
            ShipmentDto shipment = await shipmentServices.GetShipmentByIdAsync(shipmentId);
            // Prepare Notification
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

    //The function for requert to
    public async Task<Result<string>> CreateOrderAndAssignAsync(CreateOrderWithAssignmentRequest request)
    {
        var businessOwnerId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        try
        {
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
            _logger.LogError(ex, "An error occurred while creating and assigning order for Business Owner {BusinessOwnerId}.", businessOwnerId);
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


    public async Task CheckAndAssignOverdueShipments()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var currentTime = DateTime.Now.AddMinutes(-2);
        var overdueShipments = await shipmentRepository.GetList(s => s.InTransiteBeginTime.HasValue && s.InTransiteBeginTime.Value <= currentTime && s.ShipmentState != ShipmentStateEnum.Assigned && !s.IsDeleted)
            .Include(s => s.waypoints)
            .Include(s => s.Routes)
            .ToListAsync();

        foreach (var shipment in overdueShipments)
        {

            var orderIds = shipment.waypoints?.Select(w => w.orderId).ToList() ?? new List<int>();
            var orders = orderRepository.GetList(o => orderIds.Contains(o.Id) && !o.IsDeleted && o.State == OrderStateEnum.Created);
            var businessOwnerId = shipment.waypoints.FirstOrDefault().Order.BusinessID;

            var result = await AssignOrderAutomaticallyAsync(businessOwnerId, shipment);
            if (result.IsSuccess)
            {
                _logger.LogInformation($"Shipment {shipment.Id} assigned automatically.");
                continue;
            }

            _logger.LogWarning($"Automatic assignment failed for shipment {shipment.Id}: {result.Error}. Attempting forced assignment.");

            var availableRiders = await riderRepository.GetRidersForBusinessOwnerAsync(businessOwnerId);
            var suitableRiders = availableRiders.Where(r => r.VehicleStatus == "Good").ToList();

            if (suitableRiders.Any())
            {
                var firstOrder = orders.First();
                var firstOrderRoute = orderRouteRepository.GetOrderRouteByOrderID(firstOrder.Id);
                var firstRoute = await routeRepository.GetAsync(firstOrderRoute.RouteID);

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
                    orderRepository.Update(order);
                }

                shipment.ShipmentState = ShipmentStateEnum.Assigned;
                shipment.RiderID = bestRider.Rider.UserID;
                shipmentRepository.Update(shipment);
                shipmentRepository.CustomSaveChanges();
                orderRepository.CustomSaveChanges();

                bestRider.Rider.Status = RiderStatusEnum.OnDelivery;
                riderRepository.Update(bestRider.Rider);
                riderRepository.CustomSaveChanges();

                await NotifyRiderConfirmationAsync(bestRider.Rider.UserID, shipment.Id, true, "Shipment assigned due to passed delivery time.");
                _logger.LogInformation($"Shipment {shipment.Id} forcefully assigned to rider {bestRider.Rider.UserID} successfully.");
            }
            else
            {
                _logger.LogWarning($"No available riders for forced assignment of shipment {shipment.Id}. Notifying owner.");
            }
        }
    }


    public async Task CheckOrderCreatedWithoutShipments()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var currentTime = DateTime.Now;

        var orderCreatedWithoutShipment = await orderRepository.GetList(o =>
                         o.State == OrderStateEnum.Created &&
                         !o.IsDeleted &&
                         o.OrderRoute != null && o.OrderRoute.Route != null &&
                         (o.OrderRoute.Route.ShipmentID == null || o.OrderRoute.Route.Shipment.ShipmentState == ShipmentStateEnum.InTransit || o.OrderRoute.Route.Shipment.ShipmentState == ShipmentStateEnum.Delivered)
                         )
                         .Include(o => o.OrderRoute)
                         .ThenInclude(or => or.Route)
                         .ToListAsync();

        if (!orderCreatedWithoutShipment.Any())
        {
            _logger.LogInformation("مافيش طلبات معمول ليها Created أو ليها شحنة InTransit أو Delivered.");
            return;
        }

        var ordersByShipment = orderCreatedWithoutShipment.GroupBy(o => o.OrderRoute.Route.ShipmentID).ToList();

        foreach (var ordersGroup in ordersByShipment)
        {
            var shipmentId = ordersGroup.Key ?? 0;
            var orders = ordersGroup.ToList();

            var firstOrder = orders.OrderBy(o => o.Date).First();
            var firstOrderRoute = firstOrder.OrderRoute.Route;

            var lastOrderRoute = orders.Last().OrderRoute.Route;
            var shipment = await shipmentRepository.GetAsync(shipmentId);

            DateTime? inTransiteBegin;
            if (shipment != null && shipment.InTransiteBeginTime.HasValue)
            {
                // لو فيه شحنة قديمة، استخدم InTransiteBeginTime بتاعتها كقاعدة
                inTransiteBegin = shipment.InTransiteBeginTime.Value;
            }
            else
            {
                // لو مافيش شحنة قديمة، استخدم CreatedAt بتاع أول طلب
                inTransiteBegin = firstOrder.Date;
            }


            // أضف وقت بناءً على الأولوية
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
                zone = orders.First().zone,
                RiderID = null,

            };

            var newShipment = await shipmentServices.CreateShipment(addShipmentVM);

        }
    }
    public async Task<DashboardStatsDto> GetDashboardStatsAsync(string ownerUserId)
    {
        if (string.IsNullOrEmpty(ownerUserId))
        {
            throw new ArgumentException("Owner UserID cannot be empty.");
        }

        try
        {
            var stats = await businessOwnerRepo.GetDashboardStatsAsync(ownerUserId);
            return stats;
        }
        catch (Exception ex)
        {
            throw new Exception("Error fetching dashboard statistics.", ex);
        }
    }
}

