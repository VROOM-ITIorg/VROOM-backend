using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using VROOM.Repositories;

namespace VROOM.Data.Managers
{
	public class RoleRepository : BaseRepository<UserRole>
	{
		private readonly RoleManager<IdentityRole<int>> _roleManager;

		public RoleRepository(MyDbContext dbContext, RoleManager<IdentityRole<int>> roleManager)
			: base(dbContext)
		{
			_roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
		}

		public async Task<IdentityRole<int>> AddRoleAsync(string roleName)
		{
			var role = new IdentityRole<int> { Name = roleName };
			var result = await _roleManager.CreateAsync(role);
			if (result.Succeeded)
			{
				return role;
			}
			throw new Exception(""Failed to create role: "" + string.Join("", "", result.Errors.Select(e => e.Description)));
		}

		public async Task<UserRole> AssignRoleToUserAsync(int userId, string roleName)
		{
			var role = await _roleManager.FindByNameAsync(roleName);
			if (role == null)
			{
				throw new Exception($""Role '{roleName}' not found."");
			}

			var userRole = new UserRole { UserID = userId, Role = roleName };
			await _dbSet.AddAsync(userRole);
			await _dbContext.SaveChangesAsync();
			return userRole;
		}

		public async Task<List<UserRole>> GetRolesForUserAsync(int userId)
		{
			return await GetAsync(
				ur => ur.UserID == userId,
				q => q.Include(ur => ur.User));
		}

		public async Task<List<UserRole>> GetUsersInRoleAsync(string roleName)
		{
			return await GetAsync(
				ur => ur.Role == roleName,
				q => q.Include(ur => ur.User));
		}

		public async Task RemoveRoleFromUserAsync(int userId, string roleName)
		{
			var userRole = await _dbSet.FirstOrDefaultAsync(ur => ur.UserID == userId && ur.Role == roleName);
			if (userRole != null)
			{
				_dbSet.Remove(userRole);
				await _dbContext.SaveChangesAsync();
			}
		}

		public async Task DeleteRoleAsync(string roleName)
		{
			var role = await _roleManager.FindByNameAsync(roleName);
			if (role != null)
			{
				var userRoles = await GetUsersInRoleAsync(roleName);
				_dbSet.RemoveRange(userRoles);
				var result = await _roleManager.DeleteAsync(role);
				if (!result.Succeeded)
				{
					throw new Exception(""Failed to delete role: "" + string.Join("", "", result.Errors.Select(e => e.Description)));
				}
				await _dbContext.SaveChangesAsync();
			}
		}
	}
}