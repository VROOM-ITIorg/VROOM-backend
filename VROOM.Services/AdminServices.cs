using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly RiderRepository riderRepository;
        private readonly TransactionWork<Rider> transactionWork;
        private readonly TransactionWork<BusinessOwner> transactionWorkBO;
        private readonly BusinessOwnerService ownerService;
        public AdminServices(AccountManager _accountManager, BusinessOwnerRepository _ownerRepository, TransactionWork<Rider> _transactionWork, UserManager<User> _userManager, TransactionWork<BusinessOwner> _transactionWorkBO, RiderRepository _riderRepository, BusinessOwnerService _ownerService)
        {
            accountManager = _accountManager;
            ownerRepository = _ownerRepository;
            transactionWork = _transactionWork;
            userManager = _userManager;
            transactionWorkBO = _transactionWorkBO;
            riderRepository = _riderRepository;
            ownerService = _ownerService;
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
            else if (model.ImagePath != null)
            {
                return model.ImagePath = model.ImagePath;

            }
            else
            {
                return model.ImagePath = $"/Images/Rider/default-avatar-profile-icon-of-social-media-user-vector.jpg";

            }
        }

        public async Task CreateNewRider(AdminCreateRiderVM model)
        {
            var businessOwner = ownerRepository.GetBusinessDetails(model.BusinessName);

            await transactionWork.BeginTransactionAsync();
            try
            {
                var baseUserName = Regex.Replace(model.Name, @"[^a-zA-Z0-9]", "");
                var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 6);
                var uniqueUserName = $"{baseUserName}_{randomSuffix}".ToUpper();
                var newUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = model.Name,
                    Email = model.Email,
                    NormalizedEmail = model.Email.ToUpper(),
                    UserName = uniqueUserName,
                    NormalizedUserName = uniqueUserName,
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
            catch (Exception ex)
            {
                await transactionWork.RollbackAsync();
            }

        }
        public async Task CreateNewOwner(AdminCreateBusOnwerVM model)
        {
            

            await transactionWorkBO.BeginTransactionAsync();
            try
            {
                var baseUserName = Regex.Replace(model.OwnerName, @"[^a-zA-Z0-9]", "");
                var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 6);
                var uniqueUserName = $"{baseUserName}_{randomSuffix}".ToUpper();
                var newUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = model.OwnerName,
                    Email = model.Email,
                    UserName = uniqueUserName,
                    NormalizedEmail = model.Email.ToUpper(),
                    NormalizedUserName = uniqueUserName.ToUpper(),
                    EmailConfirmed = true,
                    PasswordHash = new PasswordHasher<User>().HashPassword(null, "Default@123"),
                    PhoneNumber = model.PhoneNumber,
                    Address = new() { Area = model.Address },
                };
                newUser.ProfilePicture = UploadImageProfile<AdminCreateBusOnwerVM>(model);

                var res = await userManager.CreateAsync(newUser);

                var newOwner = new BusinessOwner();
                newOwner = model.ToModel();
                newOwner.UserID = newUser.Id;
                transactionWorkBO.User.Add(newOwner);
                transactionWorkBO.User.CustomSaveChanges();
                await transactionWorkBO.CommitAsync();

                if (model.SubscriptionType == SubscriptionTypeEnum.Trial)
                {
                    await ownerService.StartTrial(newOwner.UserID);
                }
                else
                {
                    await ownerService.ActivatePaidAsync(newOwner.UserID);
                }

            }
            catch (Exception ex)
            {
                await transactionWorkBO.RollbackAsync();
            }

        }
        public async Task<(AdminEditRiderVM Rider, IEnumerable<BusinessOwner> BusinessName)> EditRider(string id)
        {
            var rider = await riderRepository.GetAsync(id);

            var viewModel = new AdminEditRiderVM
            {
                UserID = rider.UserID,
                Status = rider.Status,
                VehicleType = rider.VehicleType,
                Location = rider.Area,
                ExperienceLevel = rider.ExperienceLevel,
                UserName = rider.User?.Name,
                Email = rider.User?.Email,
                PhoneNumber = rider.User.PhoneNumber,
                ImagePath = rider.User.ProfilePicture,
                

            };
            return (Rider: viewModel, BusinessName: await ownerRepository.GetAllAsync());
        }



        public async Task EditRider(AdminEditRiderVM Rider)
        {
            var rider = await riderRepository.GetAsync(Rider.UserID);

            rider.UserID = Rider.UserID;
            rider.Status = Rider.Status;
            rider.VehicleType = Rider.VehicleType;
            rider.Area = Rider.Location;
            rider.ExperienceLevel = Rider.ExperienceLevel;
            rider.Rating = 0;
            rider.User.Name = Rider.UserName;
            rider.User.ProfilePicture = UploadImageProfile<AdminEditRiderVM>(Rider);

            riderRepository.CustomSaveChanges();
        }
        public async Task<IEnumerable<BusinessOwner>> GetAllOwners()
        {
            return await ownerRepository.GetAllAsync();
        }
        public async Task<(PaginationViewModel<AdminRiderDetialsVM> Riders, IEnumerable<BusinessOwner> owners)> ShowAllRiders(int status = -1, string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4, string sort = "name_asc", string owner = "All")
        {
            return (riderRepository.Search(status: status, Name: Name, PhoneNumber: PhoneNumber, pageNumber: pageNumber, pageSize: pageSize, sort : sort, owner : owner), await ownerRepository.GetAllAsync());
        }
        public PaginationViewModel<AdminBusOwnerDetialsVM> ShowAllOwners(int status = -1, string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4, string sort = "name_asc")
        {
            return ownerRepository.Search(Name: Name, PhoneNumber: PhoneNumber, pageNumber: pageNumber, pageSize: pageSize, sort :sort);
        }
        public async Task EditOwner(AdminEditBusOwnerVM OwnerVM)
        {
            var owner = await ownerRepository.GetAsync(OwnerVM.UserID);
            owner.UserID = OwnerVM.UserID;
            owner.BusinessType = OwnerVM.BusinessName;
            owner.User.Address.Area = OwnerVM.Address;
            owner.User.Name = OwnerVM.OwnerName;
            owner.User.Email = OwnerVM.Email;
            owner.User.ProfilePicture = UploadImageProfile<AdminEditBusOwnerVM>(OwnerVM);
            ownerRepository.CustomSaveChanges();
        }
        public async Task<AdminEditBusOwnerVM> EditOwner(string id)
        {
            var owner = await riderRepository.GetAsync(id);

            var viewModel = new AdminEditBusOwnerVM
            {
                UserID = owner.UserID,
                OwnerName = owner.User.Name,
                BusinessName = owner.BusinessOwner.BusinessType,
                Email = owner.User?.Email,
                PhoneNumber = owner.User.PhoneNumber,
                ImagePath = owner.User.ProfilePicture,
                Address = owner.User.Address?.Area

            };
            return viewModel;
        }

        public async Task Delete(string id)
        {
            var owner = await accountManager.GetAsync(id);
            owner.IsDeleted = true;
            ownerRepository.CustomSaveChanges();
        }

        public async Task SignOut()
        {
           await accountManager.Signout();
        }
    }
}
