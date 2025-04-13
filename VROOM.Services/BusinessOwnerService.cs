using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModels.User;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Repositories;
using VROOM.Repository;
using VROOM.ViewModels;

namespace VROOM.Services
{
    public class BusinessOwnerService
    {
        //private readonly MyDbContext businessOwnerRepo;
        private readonly Microsoft.AspNetCore.Identity.UserManager<User> userManager;
        private readonly BusinessOwnerRepository businessOwnerRepo;
        private readonly UserService userService;
        private readonly OrderRepository orderRepository;
        private readonly RiderRepository riderRepository;

        //private readonly SignInManager<User> _signInManager;
        public BusinessOwnerService(
            Microsoft.AspNetCore.Identity.UserManager<User> _userManager,
            BusinessOwnerRepository _businessOwnerRepo,
            OrderRepository _orderRepository,
            RiderRepository _riderRepository

            )
        {
            userManager = _userManager;
            businessOwnerRepo = _businessOwnerRepo;
            orderRepository = _orderRepository;
            riderRepository = _riderRepository;

        }

        //viewmodel
        public bool UpdateBusinessRegistration(BusinessOwner updatedBusinessInfo)
        {
            var existingBusiness = businessOwnerRepo.GetAsync(updatedBusinessInfo.UserID).Result;
            if (existingBusiness == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(updatedBusinessInfo.BankAccount))
            {
                existingBusiness.BankAccount = updatedBusinessInfo.BankAccount;

            }
            if (!string.IsNullOrEmpty(existingBusiness.BusinessType))
            {
                existingBusiness.BusinessType = updatedBusinessInfo.BusinessType;
            }


            businessOwnerRepo.CustomSaveChanges();
            return true;
        }


        public async void CreateOrder(Order order)
        {
             orderRepository.Add(order);
            orderRepository.CustomSaveChanges();
        }

        //viewmodel
        public BusinessOwnerViewModel GetBusinessDetails(string businessOwnerId)
        {
            var businessOwner = businessOwnerRepo.GetAsync(businessOwnerId).Result;

            return new BusinessOwnerViewModel
            {
                UserID = businessOwner.UserID,
                BankAccount = businessOwner.BankAccount,
                BusinessType = businessOwner.BusinessType
            }; 
        }

        

        //riderRepo
        public void CreateRiderAsync(RiderDTO model)
        {
            var rider = new Rider
            {
                UserID = model.UserID,
                BusinessID = model.BusinessID,
                Status = RiderStatusEnum.Available,
                VehicleType = model.VehicleType,
                VehicleStatus = model.VehicleStatus,
                Lat = model.Location.Lat,
                Lang = model.Location.Lang,
                Area = model.Location.Area,
                ExperienceLevel = model.ExperienceLevel,
                Rating = 0
            };

            riderRepository.Add(rider);
            riderRepository.CustomSaveChanges();
        }
        //will be refactored

        public async Task<Result<string>> ChangeRiderPasswordAsync(string riderId, string newPassword)
        {
            // Call the existing UpdatePasswordAsync method
            var updateResult = await userService.UpdatePasswordAsync(riderId, newPassword);

            // Check if the update was successful
            if (!updateResult.IsSuccess)
            {
                return Result<string>.Failure(updateResult.Error);
            }

            // If successful, return a success result with a message
            return Result<string>.Success("Password updated successfully");
        }
        //public UserDto MapToDto(User user, string role)
        //{
        //    var userDto = new UserDto
        //    {
        //        Id = user.Id,
        //        Email = user.Email,
        //        Name = user.Name,
        //        ProfilePicture = user.ProfilePicture,
        //        Role = role
        //    };

        //        if (role == RoleConstants.BusinessOwner)
        //        {
        //            var businessOwner = businessOwnerRepo.GetAsync(userDto.Id).Result;
        //            //if (businessOwner != null)
        //            //{
        //            //    userDto.BankAccount = businessOwner.BankAccount;
        //            //    userDto.BusinessType = businessOwner.BusinessType;
        //            //}
        //        }
        //        else if (role == RoleConstants.Rider)
        //        {
        //            var rider = businessOwnerRepo..Result;
        //            if (rider != null)
        //            {
        //                //riderdto??
        //            }
        //        }

        //        return userDto;
        //    }
        //public async Task<ApiResponse<OrderRider>> AssignOrderToRider(int orderId, string riderId, string businessOwnerId)
        //{
        //    var response = new ApiResponse<OrderRider>();

        //    try
        //    {

        //        var businessOwner = businessOwnerRepo.GetAsync(businessOwnerId);
        //        if (businessOwner == null)
        //        {
        //            return new ApiResponse<OrderRider> { Success = false, Message = "Business owner not found." };
        //        }


        //        var order = orderRepository.GetAsync(orderId);
        //        if (order == null)
        //        {
        //            return new ApiResponse<OrderRider> { Success = false, Message = "Order not found." };
        //        }


        //        var rider = businessOwnerRepo.Riders.FirstOrDefault(r => r.UserID == riderId);
        //        if (rider == null || rider.BusinessID != businessOwner.UserID)
        //        {
        //            return new ApiResponse<OrderRider> { Success = false, Message = "Rider not found or doesn't belong to your business." };
        //        }


        //        if (rider.Status != RiderStatus.Available)
        //        {
        //            return new ApiResponse<OrderRider> { Success = false, Message = "rider is not available for assignment." };
        //        }



        //        var orderRider = new OrderRider
        //        {
        //            OrderID = orderId,
        //            RiderID = riderId
        //        };

        //        await businessOwnerRepo.OrderRiders.AddAsync(orderRider); //oderrider


        //        order.RiderID = riderId;
        //        order.State = OrderState.Confirmed;
        //        order.ModifiedBy = businessOwnerId.ToString();
        //        order.ModifiedAt = DateTime.Now;
        //        businessOwnerRepo.Orders.Update(order);

        //        rider.Status = RiderStatus.OnDelivery;
        //        businessOwnerRepo.Riders.Update(rider);

        //        await businessOwnerRepo.SaveChangesAsync();

        //        return new ApiResponse<OrderRider>
        //        {
        //            Success = true,
        //            Message = "Order successfully assigned to rider.",
        //            Data = orderRider
        //        };
        //    }

        //    catch (Exception ex)
        //    {
        //        return new ApiResponse<OrderRider> { Success = false, Message = $"An error occurred: {ex.Message}" };
        //    }

        //}
    }
}
