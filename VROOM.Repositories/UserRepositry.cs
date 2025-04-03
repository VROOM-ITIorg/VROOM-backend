using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    public class UserRepository : BaseRepository<User>
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly MyDbContext _dbContext; // Add this to access DbContext directly when needed

        public UserRepository(MyDbContext dbContext, UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager)
            : base(dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        // Override GetAsync to handle string ID (Identity uses string IDs)
        public virtual async Task<User> GetAsync(string id)
        {
            return await _userManager.FindByIdAsync(id);
        }

        // Override base GetAsync(int id) since it won't work with Identity's string IDs
        public override async Task<User> GetAsync(int id)
        {
            throw new NotSupportedException("UserRepository uses string IDs. Use GetAsync(string id) instead.");
        }

        public override async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _dbContext.Users
                .Where(u => !u.IsDeleted)
                .ToListAsync();
        }

        public override async Task AddAsync(User user)
        {
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception("Failed to create user: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task<User> AddUserWithPasswordAsync(User user, string password)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                return user;
            }
            throw new Exception("Failed to create user: " +
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<User?> GetUserWithDetailsAsync(string userId)
        {
            return await _dbContext.Users
                .Include(u => u.Address)
                .Include(u => u.Notifications)
                .Include(u => u.Customer)
                .Include(u => u.BusinessOwner)
                .Include(u => u.Rider)
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        }

        public async Task<List<User>> GetUsersByRoleAsync(string role)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            return usersInRole
                .Where(u => !u.IsDeleted)
                .ToList();
        }

        public override void Update(User user)
        {
            user.ModifiedAt = DateTime.UtcNow;
            var result = _userManager.UpdateAsync(user).GetAwaiter().GetResult();
            if (!result.Succeeded)
            {
                throw new Exception("Failed to update user: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public override void Delete(User user)
        {
            user.IsDeleted = true;
            user.ModifiedAt = DateTime.UtcNow;
            Update(user);
            // For hard delete, use: _userManager.DeleteAsync(user).GetAwaiter().GetResult();
        }

        public async Task AssignRoleToUserAsync(int userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new Exception($"User with ID {userId} not found.");
            }

            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                throw new Exception($"Role '{roleName}' not found.");
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                throw new Exception("Failed to assign role: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task<IList<string>> GetRolesForUserAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new Exception($"User with ID {userId} not found.");
            }
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<List<User>> GetUsersInRoleAsync(string roleName)
        {
            var users = await _userManager.GetUsersInRoleAsync(roleName);
            return users.Where(u => !u.IsDeleted).ToList();
        }

        public async Task RemoveRoleFromUserAsync(int userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new Exception($"User with ID {userId} not found.");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                throw new Exception("Failed to remove role: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}