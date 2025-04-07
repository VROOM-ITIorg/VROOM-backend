using Microsoft.AspNetCore.Identity;
using VROOM.Data;

namespace VROOM.Repositories
{
    public class RoleRepository : BaseRepository<IdentityRole>
    {
        RoleManager<IdentityRole> _roleManager;
        public RoleRepository(VroomDbContext context) : base(context){}

    }
}
