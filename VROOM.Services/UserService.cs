using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Repository;
using Microsoft.Extensions.Configuration;
using ViewModels.User;
using Azure.Core;

namespace VROOM.Services
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string? Error { get; }

        private Result(bool isSuccess, T value, string? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new Result<T>(true, value, null);
        public static Result<T> Failure(string error) => new Result<T>(false, default, error);
    }

    public static class RoleConstants
    {
        public const string Customer = "Customer";
        public const string BusinessOwner = "BusinessOwner";
        public const string Rider = "Rider";
        public const string Admin = "Admin";
    }

    public class UserService
    {
        private readonly UserRepository _userRepository;
        private readonly string _jwtSecret;
        private readonly ILogger<UserService> _logger;

        // Error message constants
        private const string EmailExistsError = "Email already exists.";
        private const string RoleNotFoundError = "No role found.";
        private const string InvalidRoleError = "Invalid role specified.";
        private const string BusinessOwnerValidationError = "BankAccount and BusinessType are required for BusinessOwner.";
        private const string RegistrationError = "An error occurred during registration.";

        public UserService(
            UserRepository userRepository,
            IConfiguration configuration,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _jwtSecret = "ShampooShampooShampooShampooShampooShampoo" ?? throw new ArgumentNullException("Jwt:Secret is missing in configuration.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // 1. User Registration (Customer, BusinessOwner)
        public async Task<Result<UserDto>> RegisterAsync(RegisterRequest request)
        {
            _logger.LogInformation("Registering user with email: {Email}, Role: {Role}", request.Email, request.Role);

            // Validate role (only allow Customer, BusinessOwner)
            if (request.Role == RoleConstants.Rider)
            {
                _logger.LogWarning("Registration failed: Rider registration is not allowed through this endpoint.");
                return Result<UserDto>.Failure("Rider registration must be handled through BusinessOwnerService.");
            }
            if (request.Role == RoleConstants.Admin)
            {
                _logger.LogWarning("Registration failed: Admin registration is not allowed through this endpoint.");
                return Result<UserDto>.Failure("Admin registration must be handled through BusinessOwnerService.");
            }

            // Validate email uniqueness
            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists.", request.Email);
                return Result<UserDto>.Failure(EmailExistsError);
            }

            // Validate role existence
            if (!await _userRepository.RoleExistsAsync(request.Role))
            {
                _logger.LogWarning("Registration failed: No role found with the name provided for email: {Email}", request.Email);
                return Result<UserDto>.Failure(RoleNotFoundError);
            }

            // Validate role-specific requirements
            var validationResult = ValidateRoleSpecificRequirements(request);
            if (!validationResult.IsSuccess)
            {
                return Result<UserDto>.Failure(validationResult.Error);
            }

            // Create user and related entities within a transaction
            try
            {
                await _userRepository.BeginTransactionAsync();

                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = request.Email,
                    Email = request.Email,
                    Name = request.Name,
                    ProfilePicture = request.ProfilePicture,
                    ModifiedAt = DateTime.Now,
                    IsDeleted = false
                };

                var result = await _userRepository.CreateUserAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("User creation failed for email {Email}: {Errors}", request.Email, errors);
                    await _userRepository.RollbackTransactionAsync();
                    return Result<UserDto>.Failure(errors);
                }

                await _userRepository.AddUserToRoleAsync(user, request.Role);

                switch (request.Role)
                {
                    case RoleConstants.Customer:
                        await _userRepository.AddCustomerAsync(new Customer { UserID = user.Id, User = user });
                        break;

                    case RoleConstants.BusinessOwner:
                        await _userRepository.AddBusinessOwnerAsync(new BusinessOwner
                        {
                            UserID = user.Id,
                            User = user,
                            BankAccount = request.BankAccount!,
                            BusinessType = request.BusinessType!
                        });
                        break;

                    default:
                        _logger.LogWarning("Registration failed: Invalid role {Role}.", request.Role);
                        await _userRepository.RollbackTransactionAsync();
                        return Result<UserDto>.Failure(InvalidRoleError);
                }

                if (request.Address != null)
                {
                    await _userRepository.AddAddressAsync(new Address
                    {
                        UserID = user.Id,
                        User = user,
                        Lat = request.Address.Lat,
                        Lang = request.Address.Lang,
                        Area = request.Address.Area,
                        ModifiedAt = DateTime.Now,
                        IsDeleted = false
                    });
                }

                await _userRepository.CommitTransactionAsync();

                _logger.LogInformation("User {Email} registered successfully with role {Role}.", request.Email, request.Role);
                return Result<UserDto>.Success(MapToDto(user, request.Role));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while registering user {Email}.", request.Email);
                await _userRepository.RollbackTransactionAsync();
                return Result<UserDto>.Failure(RegistrationError);
            }
        }

        // 2. User Login
        public async Task<Result<string>> LoginAsync(string email, string password)
        {
            var user = await _userRepository.FindUserByEmailAsync(email);
            if (user == null || user.IsDeleted)
            {
                _logger.LogWarning("Login failed: Invalid email or password for email {Email}.", email);
                return Result<string>.Failure("Invalid email or password.");
            }

            var isPasswordValid = await _userRepository.CheckPasswordAsync(user, password);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed: Invalid email or password for email {Email}.", email);
                return Result<string>.Failure("Invalid email or password.");
            }

            var token = await GenerateJwtToken(user);
            _logger.LogInformation("User {Email} logged in successfully.", email);
            return Result<string>.Success(token);
        }



        // 3. Role Management
        public async Task<Result<UserDto>> AssignRoleAsync(string userId, string role)
        {
            var user = await _userRepository.FindUserByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                _logger.LogWarning("AssignRole failed: User with ID {UserId} not found or is deleted.", userId);
                return Result<UserDto>.Failure("User not found.");
            }

            if (!await _userRepository.RoleExistsAsync(role))
            {
                await _userRepository.CreateRoleAsync(new IdentityRole(role));
            }

            var currentRoles = await _userRepository.GetUserRolesAsync(user);
            await _userRepository.RemoveUserFromRolesAsync(user, currentRoles);
            await _userRepository.AddUserToRoleAsync(user, role);

            await _userRepository.RemoveCustomerAsync(user.Id);
            await _userRepository.RemoveBusinessOwnerAsync(user.Id);
            await _userRepository.RemoveRiderAsync(user.Id);

            switch (role)
            {
                case RoleConstants.Customer:
                    await _userRepository.AddCustomerAsync(new Customer { UserID = user.Id, User = user });
                    break;

                case RoleConstants.BusinessOwner:
                    await _userRepository.AddBusinessOwnerAsync(new BusinessOwner
                    {
                        UserID = user.Id,
                        User = user,
                        BankAccount = "",
                        BusinessType = ""
                    });
                    break;

                case RoleConstants.Rider:
                    await _userRepository.AddRiderAsync(new Rider
                    {
                        UserID = user.Id,
                        User = user,
                        BusinessID = "",
                        Status = RiderStatusEnum.Unavailable,
                        VehicleType = VehicleTypeEnum.Motorcycle,
                        VehicleStatus = VehicleTypeStatus.Unknowen,
                        Lang = 0,
                        Lat = 0,
                        Area = "Unknown",
                        ExperienceLevel = 0,
                        Rating = 0
                    });
                    break;

               
                default:
                    _logger.LogWarning("AssignRole failed: Invalid role {Role} for user {UserId}.", role, userId);
                    return Result<UserDto>.Failure("Invalid role specified.");
            }

            user.ModifiedAt = DateTime.Now;
            user.ModifiedBy = userId;
            await _userRepository.UpdateUserAsync(user);

            _logger.LogInformation("Role {Role} assigned to user {UserId} successfully.", role, userId);
            return Result<UserDto>.Success(MapToDto(user, role));
        }

        // 4. Profile Management - Update Profile
        public async Task<Result<UserDto>> UpdateProfileAsync(string userId, string name, string profilePicture, AddressDto? addressDto)
        {
            var user = await _userRepository.FindUserByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                _logger.LogWarning("UpdateProfile failed: User with ID {UserId} not found or is deleted.", userId);
                return Result<UserDto>.Failure("User not found.");
            }

            user.Name = name;
            user.ProfilePicture = profilePicture;
            user.ModifiedAt = DateTime.Now;
            user.ModifiedBy = userId;

            if (addressDto != null)
            {
                if (user.Address != null)
                {
                    user.Address.Lat = addressDto.Lat;
                    user.Address.Lang = addressDto.Lang;
                    user.Address.Area = addressDto.Area;
                    user.Address.ModifiedAt = DateTime.Now;
                    user.Address.ModifiedBy = userId;
                    user.Address.IsDeleted = false;
                }
                else
                {
                    var address = new Address
                    {
                        UserID = userId,
                        User = user,
                        Lat = addressDto.Lat,
                        Lang = addressDto.Lang,
                        Area = addressDto.Area,
                        ModifiedAt = DateTime.Now,
                        ModifiedBy = userId,
                        IsDeleted = false
                    };
                    await _userRepository.AddAddressAsync(address);
                }
            }

            await _userRepository.UpdateUserAsync(user);
            await _userRepository.SaveChangesAsync();

            var roles = await _userRepository.GetUserRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "";
            _logger.LogInformation("Profile updated successfully for user {UserId}.", userId);
            return Result<UserDto>.Success(MapToDto(user, role));
        }

        // 5. Profile Management - Update Password
        public async Task<Result<UserDto>> UpdatePasswordAsync(string userId, string newPassword)
        {
            var user = await _userRepository.FindUserByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                _logger.LogWarning("UpdatePassword failed: User with ID {UserId} not found or is deleted.", userId);
                return Result<UserDto>.Failure("User not found.");
            }

            var token = await _userRepository.GeneratePasswordResetTokenAsync(user);
            var result = await _userRepository.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("UpdatePassword failed for user {UserId}: {Errors}", userId, errors);
                return Result<UserDto>.Failure(errors);
            }

            user.ModifiedAt = DateTime.Now;
            user.ModifiedBy = userId;
            await _userRepository.UpdateUserAsync(user);

            var roles = await _userRepository.GetUserRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "";
            _logger.LogInformation("Password updated successfully for user {UserId}.", userId);
            return Result<UserDto>.Success(MapToDto(user, role));
        }

        // 6. Add customer when the bussnisowner create new order 

        public async Task<Customer> AddNewCustomerAsync(CustomerAddViewModel CustomerAddVM)
        {
            // Validate email uniqueness
            if (await _userRepository.EmailExistsAsync(CustomerAddVM.Username))
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists.", CustomerAddVM.Username);
                return null;
            }
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = CustomerAddVM.Username,
                Email = CustomerAddVM.Username,
                Name = CustomerAddVM.Name,
                ModifiedAt = DateTime.Now,
                IsDeleted = false
            };
            await _userRepository.BeginTransactionAsync();

            var result = await _userRepository.CreateUserAsync(user, "Cust@123456");

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("User creation failed for email {Email}: {Errors}", CustomerAddVM.Username, errors);
                await _userRepository.RollbackTransactionAsync();
                return null;
            }
            await _userRepository.AddUserToRoleAsync(user, RoleConstants.Customer);

            // Add the bussnisowner id 
            Customer newCustomer = new Customer { UserID = user.Id, User = user };

            await _userRepository.AddCustomerAsync(newCustomer);
            await _userRepository.CommitTransactionAsync();
            return newCustomer;
        }
          private Result<bool> ValidateRoleSpecificRequirements(RegisterRequest request)
        {
            switch (request.Role)
            {
                case RoleConstants.Customer:
                    return Result<bool>.Success(true);

                case RoleConstants.BusinessOwner:
                    if (string.IsNullOrEmpty(request.BankAccount) || string.IsNullOrEmpty(request.BusinessType))
                    {
                        _logger.LogWarning("BusinessOwner registration failed: BankAccount and BusinessType are required.");
                        return Result<bool>.Failure(BusinessOwnerValidationError);
                    }
                    return Result<bool>.Success(true);

                case RoleConstants.Admin:
                    return Result<bool>.Success(true);

                default:
                    _logger.LogWarning("Registration failed: Invalid role {Role}.", request.Role);
                    return Result<bool>.Failure(InvalidRoleError);
            }
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var roles = await _userRepository.GetUserRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email)
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: "VROOM",
                audience: "VROOM",
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private UserDto MapToDto(User user, string role)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                ProfilePicture = user.ProfilePicture,
                Address = user.Address,
                Role = role
            };
        }



       
    }

    public record RegisterRequest(
        string Email,
        string Password,
        string Name,
        string? ProfilePicture,
        string Role,
        AddressDto? Address,
        string? BankAccount,
        string? BusinessType,
        string? BusinessID,
        RiderStatusEnum? RiderType,
        VehicleTypeEnum? VehicleType,
        string? VehicleStatus,
        double? Lang,
        double? Lat,
        string? Area,
        float? ExperienceLevel
    );

}