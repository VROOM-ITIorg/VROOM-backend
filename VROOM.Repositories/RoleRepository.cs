using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VROOM.Repositories;

namespace VROOM.Data.Managers
{
    public class RoleRepository : BaseRepository<IdentityRole<int>> // Changed to IdentityRole<int>
    {
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly MyDbContext _dbContext; // Added for direct access

        public RoleRepository(MyDbContext dbContext, RoleManager<IdentityRole<int>> roleManager)
            : base(dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        // Add a new role
        public async Task<IdentityRole<int>> AddRoleAsync(string roleName)
        {
            var role = new IdentityRole<int> { Name = roleName };
            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                return role;
            }
            throw new Exception("Failed to create role: " +
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Get role by ID (override base method)
        public override async Task<IdentityRole<int>> GetAsync(int id)
        {
            return await _roleManager.FindByIdAsync(id.ToString());
        }

        // Get all roles
        public override async Task<IEnumerable<IdentityRole<int>>> GetAllAsync()
        {
            return await _roleManager.Roles.ToListAsync();
        }

        // Delete a role
        public async Task DeleteRoleAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                throw new Exception($"Role '{roleName}' not found.");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                throw new Exception("Failed to delete role: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}