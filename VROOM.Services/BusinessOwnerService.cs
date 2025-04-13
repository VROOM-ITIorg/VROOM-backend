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
using ViewModels.Rider;
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
        private readonly ILogger<BusinessOwnerService> _logger;
        private readonly Microsoft.AspNetCore.Identity.UserManager<User> userManager;
        private readonly BusinessOwnerRepository businessOwnerRepo;
        private readonly UserService userService;
        private readonly OrderRepository orderRepository;
        private readonly RiderRepository riderRepository;
        private readonly Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> _roleManager;
        private readonly UserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserRepository _userRepository; // New dependency

        public BusinessOwnerService(
            Microsoft.AspNetCore.Identity.UserManager<User> _userManager,
            BusinessOwnerRepository _businessOwnerRepo,
            OrderRepository _orderRepository,
            RiderRepository _riderRepository,
            RoleManager<IdentityRole> roleManager,
            UserService userService,
   
            ILogger<BusinessOwnerService> logger,
            IHttpContextAccessor httpContextAccessor,
            UserRepository userRepository) // Add UserRepository to constructor
        {
            userManager = _userManager;
            businessOwnerRepo = _businessOwnerRepo;
            orderRepository = _orderRepository;
            riderRepository = _riderRepository;
            _roleManager = roleManager;
      
            _userService = userService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
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

       

        /// <summary>
        /// Retrieves a list of customers who have placed orders managed by the Business Owner's riders.
        /// </summary>
        /// <returns>A Result containing a list of CustomerVMs or an error message if the operation fails.</returns>
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
    }
}