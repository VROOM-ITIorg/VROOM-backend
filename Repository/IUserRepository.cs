using Microsoft.AspNetCore.Identity;
using VROOM.Models;

namespace VROOM.Repositories
{
    public interface IUserRepository
    {
        Task<bool> EmailExistsAsync(string email);
        Task<IdentityResult> CreateUserAsync(User user, string password);
        Task<User?> FindUserByEmailAsync(string email);
        Task<User?> FindUserByIdAsync(string userId);
        Task<bool> CheckPasswordAsync(User user, string password);
        Task<bool> RoleExistsAsync(string role);
        Task<IdentityResult> CreateRoleAsync(IdentityRole role);
        Task<IList<string>> GetUserRolesAsync(User user);
        Task<IdentityResult> RemoveUserFromRolesAsync(User user, IEnumerable<string> roles);
        Task<IdentityResult> AddUserToRoleAsync(User user, string role);
        Task AddCustomerAsync(Customer customer);
        Task AddBusinessOwnerAsync(BusinessOwner businessOwner);
        Task AddRiderAsync(Rider rider);
        Task AddAddressAsync(Address address);
        Task RemoveCustomerAsync(string userId);
        Task RemoveBusinessOwnerAsync(string userId);
        Task RemoveRiderAsync(string userId);
        Task<IdentityResult> UpdateUserAsync(User user);
        Task<string> GeneratePasswordResetTokenAsync(User user);
        Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword);
        Task SaveChangesAsync();
    }
}
