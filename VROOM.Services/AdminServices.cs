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
        private readonly UserManager<User> userManager;
        private readonly BusinessOwnerRepository ownerRepository;
        private readonly TransactionWork<Rider> transactionWork;
        private readonly TransactionWork<BusinessOwner> transactionWorkBO;

        public AdminServices( AccountManager _accountManager,BusinessOwnerRepository _ownerRepository, TransactionWork<Rider> _transactionWork, UserManager<User> _userManager, TransactionWork<BusinessOwner> _transactionWorkBO)
        {
            accountManager = _accountManager;
            ownerRepository = _ownerRepository;
            transactionWork = _transactionWork;
            userManager = _userManager;
            transactionWorkBO = _transactionWorkBO;
        }
        public async Task<SignInResult> Login(LoginViewModel user)
        {
            return await accountManager.Login(user);
        }


        public string UploadImageProfile<T>(T model) where T : UserProfile
        {
            if (model.ProfilePicture != null)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Rider");
                Directory.CreateDirectory(uploadsFolder);

                var fileExt = Path.GetExtension(model.ProfilePicture.FileName);
                var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}{fileExt}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    model.ProfilePicture.CopyTo(fileStream);
                }

               return model.ImagePath = $"/Images/Rider/{fileName}";
            }
            return null;
        }
        public async Task CreateNewRider(AdminCreateRiderVM model) 
        {
            var businessOwner = ownerRepository.GetBusinessDetails(model.BusinessName);

            await transactionWork.BeginTransactionAsync();
            try
            {
                var newUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = model.UserName,
                    Email = model.Email,
                    UserName = model.UserName.ToUpper(),
                    NormalizedEmail = model.Email.ToUpper(),
                    NormalizedUserName = model.Email.ToUpper(),
                    EmailConfirmed = true,
                    PasswordHash = new PasswordHasher<User>().HashPassword(null, "Default@123"),
                    PhoneNumber = model.PhoneNumber
                };
                newUser.ProfilePicture = UploadImageProfile<AdminCreateRiderVM>(model);
                
                var res = await userManager.CreateAsync(newUser);
                
                var newRider = new Rider();
                newRider = model.ToModel();
                newRider.UserID = newUser.Id;
                newRider.BusinessID = businessOwner.UserID;
                transactionWork.User.Add(newRider);
                transactionWork.User.CustomSaveChanges();
                await transactionWork.CommitAsync();
            }
            catch (Exception ex) {
                await transactionWork.RollbackAsync();
            }

        }

        public async Task CreateNewOwner(AdminCreateBusOnwerVM model)
        {

            await transactionWorkBO.BeginTransactionAsync();
            try
            {
                var newUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = model.OwnerName,
                    Email = model.Email,
                    UserName = model.OwnerName.ToUpper(),
                    NormalizedEmail = model.Email.ToUpper(),
                    NormalizedUserName = model.Email.ToUpper(),
                    EmailConfirmed = true,
                    PasswordHash = new PasswordHasher<User>().HashPassword(null, "Default@123"),
                    PhoneNumber = model.PhoneNumber,
                    Address = new() { Area = model.Address},
                };
                newUser.ProfilePicture = UploadImageProfile<AdminCreateBusOnwerVM>(model);

                var res = await userManager.CreateAsync(newUser);

                var newOwner = new BusinessOwner();
                newOwner = model.ToModel();
                newOwner.UserID = newUser.Id;
                transactionWorkBO.User.Add(newOwner);
                transactionWorkBO.User.CustomSaveChanges();
                await transactionWorkBO.CommitAsync();
            }
            catch (Exception ex)
            {
                await transactionWorkBO.RollbackAsync();
            }

        }

    }
}
