using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;

namespace VROOM.Services
{
    public class BusinessOwnerService
    {
        private readonly MyDbContext _dbContext;

        BusinessOwnerRepository businessOwnerManager;

        OrderRepository orderRepository;

        public BusinessOwnerService(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public BusinessOwnerService(BusinessOwnerRepository _businessOwnerManager , OrderRepository _orderRepository)
        {
            businessOwnerManager = _businessOwnerManager;
            orderRepository = _orderRepository;

        }
      
        public bool UpdateBusinessRegistration(int businessOwnerId, BusinessOwner updatedBusinessInfo)
        {
            var existingBusiness = _dbContext.BusinessOwners.FirstOrDefault(b => b.Id == businessOwnerId);
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
           
           
            _dbContext.SaveChanges();
            return true;
        }


        public void CreateOrder (Order order)
        {
            orderRepository.AddAsync(order);
        }


        public BusinessOwner GetBusinessDetails(int businessOwnerId)
        {
            return _dbContext.BusinessOwners.FirstOrDefault(b=>b.Id == businessOwnerId);
        }


        //riderRepo
        public void CreateRider(Rider rider)
        {
            //riderRepo.Add(rider)
            

        }
        public bool AssignRiderManual(int businessOwnerId, int riderId)
        {
            var businessOwner = _dbContext.BusinessOwners.FirstOrDefault(b => b.Id == businessOwnerId);
            if (businessOwner == null)
                return false;

            var rider = _dbContext.Riders.FirstOrDefault(r => r.Id == riderId);
            if (rider == null)
                return false;

            var riderAssignment = new RiderAssignment
            {
                BusinessID = businessOwnerId,
                RiderID = riderId,
                AssignmentDate = DateTime.UtcNow
            };

            _dbContext.RiderAssignments.Add(riderAssignment);
            _dbContext.SaveChanges();
            return true;

        }

    }
}
