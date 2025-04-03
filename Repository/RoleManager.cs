using Microsoft.AspNetCore.Identity;
using VROOM.Data;

namespace VROOM.Repositories
{
    public class RoleManager : BaseManager<IdentityRole>
    {
        RoleManager<IdentityRole> _roleManager;
        public RoleManager(VroomDbContext context, RoleManager<IdentityRole> roleManager) : base(context)
        {
            _roleManager = roleManager;
        }


        public async Task<IdentityResult> Add(string rolename)
        {
            return await _roleManager.CreateAsync(new IdentityRole { Name = rolename });
        }
    }
}
