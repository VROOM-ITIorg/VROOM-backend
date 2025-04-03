using Microsoft.AspNetCore.Identity;
using VROOM.Data;
using VROOM.Models;


namespace VROOM.Repositories
{
    public class AccountManager : BaseManager<User>
    {
        private UserManager<User> UserManager;
        private SignInManager<User> signInManager;
        public AccountManager(
            VroomDbContext context,
            UserManager<User> _UserManager,
            SignInManager<User> _signInManager
            )
            : base(context)
        {
            UserManager = _UserManager;
            signInManager = _signInManager;

        }


        //public async Task<IdentityResult> Register(UserRegisterVM userRegister)
        //{
        //    return await UserManager.CreateAsync(userRegister.ToModel(), userRegister.Password);
        //}

        //public async Task<SignInResult> Login(UserLoginVM vmodel)
        //{
        //    //if correct Email
        //    var User = await UserManager.FindByEmailAsync(vmodel.Method);
        //    if (User != null)
        //        return await signInManager.PasswordSignInAsync(User, vmodel.Password, true, true);
        //    else
        //        return await signInManager.PasswordSignInAsync(vmodel.Method, vmodel.Password, true, true);
        //}
    }
}
