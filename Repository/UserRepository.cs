
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;

namespace VROOM.Repository
{
    public class UserRepository : BaseRepository<User>
    {
        private readonly UserManager<User> userManager;
        private readonly SignInManager<User> signInManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private IDbContextTransaction _transaction;

        public UserRepository(
            UserManager<User> _userManager,
            SignInManager<User> _signInManager,
            RoleManager<IdentityRole> _roleManager,
            VroomDbContext _context) : base(_context)
        {
            userManager = _userManager ?? throw new ArgumentNullException(nameof(userManager));
            signInManager = _signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            roleManager = _roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        // Transaction Management
        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress.");
            }
            _transaction = await context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction is in progress.");
            }
            await _transaction.CommitAsync();
            _transaction.Dispose();
            _transaction = null;
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction is in progress.");
            }
            await _transaction.RollbackAsync();
            _transaction.Dispose();
            _transaction = null;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<IdentityResult> CreateUserAsync(User user, string password)
        {
            return await userManager.CreateAsync(user, password);
        }

        public async Task<User?> FindUserByEmailAsync(string email)
        {
            return await userManager.FindByEmailAsync(email);
        }

        public async Task<User?> FindUserByIdAsync(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user = await context.Users
                    .Include(u => u.Address)
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            return user;
        }

        public async Task<bool> CheckPasswordAsync(User user, string password)
        {
            var result = await signInManager.CheckPasswordSignInAsync(user, password, false);
            return result.Succeeded;
        }

        public async Task<bool> RoleExistsAsync(string role)
        {
            return await roleManager.RoleExistsAsync(role);
        }

        public async Task<IdentityResult> CreateRoleAsync(IdentityRole role)
        {
            return await roleManager.CreateAsync(role);
        }

        public async Task<IList<string>> GetUserRolesAsync(User user)
        {
            return await userManager.GetRolesAsync(user);
        }

        public async Task<IdentityResult> RemoveUserFromRolesAsync(User user, IEnumerable<string> roles)
        {
            return await userManager.RemoveFromRolesAsync(user, roles);
        }

        public async Task<IdentityResult> AddUserToRoleAsync(User user, string role)
        {
            return await userManager.AddToRoleAsync(user, role);
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            context.Customers.Add(customer);
            await context.SaveChangesAsync();
        }

        public async Task AddBusinessOwnerAsync(BusinessOwner businessOwner)
        {
            context.BusinessOwners.Add(businessOwner);
            await context.SaveChangesAsync();
        }

        public async Task AddRiderAsync(Rider rider)
        {
            context.Riders.Add(rider);
            await context.SaveChangesAsync();
        }

        public async Task AddAddressAsync(Address address)
        {
            context.Addresses.Add(address);
            await context.SaveChangesAsync();
        }

        public async Task RemoveCustomerAsync(string userId)
        {
            var customer = await context.Customers.FirstOrDefaultAsync(c => c.UserID == userId);
            if (customer != null)
            {
                context.Customers.Remove(customer);
                await context.SaveChangesAsync();
            }
        }

        public async Task RemoveBusinessOwnerAsync(string userId)
        {
            var businessOwner = await context.BusinessOwners.FirstOrDefaultAsync(bo => bo.UserID == userId);
            if (businessOwner != null)
            {
                context.BusinessOwners.Remove(businessOwner);
                await context.SaveChangesAsync();
            }
        }

        public async Task RemoveRiderAsync(string userId)
        {
            var rider = await context.Riders.FirstOrDefaultAsync(r => r.UserID == userId);
            if (rider != null)
            {
                context.Riders.Remove(rider);
                await context.SaveChangesAsync();
            }
        }

        public async Task<IdentityResult> UpdateUserAsync(User user)
        {
            return await userManager.UpdateAsync(user);
        }

        public async Task<string> GeneratePasswordResetTokenAsync(User user)
        {
            return await userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword)
        {
            return await userManager.ResetPasswordAsync(user, token, newPassword);
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }
    }
}
