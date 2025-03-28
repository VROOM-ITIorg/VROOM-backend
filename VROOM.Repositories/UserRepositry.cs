using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VROOM.Models;
using VROOM.Data;
namespace VROOM.Repositories
{
    public class UserRepository : BaseRepository<User>
    {
        private readonly BaseRepository<User> _userManager;

        public UserRepository(MyDbContext dbContext, BaseRepository<User> userManager)
            : base(dbContext)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public override async Task<User> AddAsync(User user)
        {
            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                return user;
            }
            throw new Exception(""Failed to create user: "" + string.Join("", "", result.Errors.Select(e => e.Description)));
        }

        public async Task<User> AddUserWithPasswordAsync(User user, string password)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                return user;
            }
            throw new Exception(""Failed to create user: "" + string.Join("", "", result.Errors.Select(e => e.Description)));
        }

        public async Task<User> GetUserWithDetailsAsync(int userId)
        {
            var user = await GetAsync(
                u => u.Id == userId, 
                q => q.Include(u => u.Address)
                      .Include(u => u.Notifications)
                      .Include(u => u.Customer)
                      .Include(u => u.BusinessOwner)
                      .Include(u => u.Rider)).FirstOrDefaultAsync();

            if (user != null)
            {
                user.UserRoles = (await _userManager.GetRolesAsync(user)).Select(r => new UserRole { UserID = user.Id, Role = r }).ToList();
            }
            return user;
        }

        public async Task<List<User>> GetUsersByRoleAsync(string role)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            return await GetAsync(
                u => usersInRole.Select(ur => ur.Id).Contains(u.Id),
                q => q.Include(u => u.Address));
        }

        public override async Task UpdateAsync(User user)
        {
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception(""Failed to update user: "" + string.Join("", "", result.Errors.Select(e => e.Description)));
            }
        }

        public override async Task DeleteAsync(User user)
        {
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception(""Failed to delete user: "" + string.Join("", "", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
}
