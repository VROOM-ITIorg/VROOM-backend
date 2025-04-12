using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using ViewModels;
using ViewModels.Account;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Repository;
using VROOM.ViewModels;

namespace VROOM.Services
{
    public class AdminServices
    {
        private readonly AccountManager accountManager;
        private readonly UserRepository userRepository;
        private readonly TransactionWork<Rider> User;
        
        public AdminServices( AccountManager _accountManager, UserRepository _userRepository, TransactionWork<Rider> _User)
        {
            accountManager = _accountManager;
            userRepository = _userRepository;
            User = _User;
        }
        public async Task<SignInResult> Login(LoginViewModel user)
        {
            return await accountManager.Login(user);
        }




    }
}
