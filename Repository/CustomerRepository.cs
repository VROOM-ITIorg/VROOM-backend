using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.HttpResults;

namespace VROOM.Repository
{
    public class CustomerRepository : BaseRepository<Customer>
    {
        private readonly UserManager<User> userManager;
        public CustomerRepository(VroomDbContext _context, UserManager<User> _userManager) : base(_context) {
            userManager = _userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<Customer> GetByUsernameAsync(string username)
        {
            // EF.Property<string>(e, "Email") this for weak entity because we don't know the property in the compile time 
            return await dbSet.FirstOrDefaultAsync(e => e.User.UserName == username);
        }

        //public async Task<Customer> FindCustomerByEmailAsync(string email)
        //{
        //    return await userManager.FindByEmailAsync(email);
        //}


        public async Task CustomSaveChangesAsync()
        {
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("An error occurred while saving changes to the database.", ex);
            }
        }


    }
}
