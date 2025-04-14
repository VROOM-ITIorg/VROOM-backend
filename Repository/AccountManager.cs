using Microsoft.AspNetCore.Identity;
using ViewModels.Account;
using VROOM.Data;
using VROOM.Models;


namespace VROOM.Repositories
{
    public class AccountManager : BaseRepository<User>
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

        public async Task<SignInResult> Login(LoginViewModel vmodel)
        {
            var User = await UserManager.FindByEmailAsync(vmodel.Email);
            if(User != null)
             return await signInManager.PasswordSignInAsync(User, vmodel.Password, vmodel.RememberMe,true);
            else
                return SignInResult.Failed;
        }

        public async Task Signout()
        {
            await signInManager.SignOutAsync();
        }
    }
}
