using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;


namespace VROOM.Repositories
{
    
        public class BusinessOwnerManager
        {
            private readonly MyDbContext _dbContext;
            public BusinessOwnerManager(MyDbContext dbContext)
            {
                _dbContext = dbContext;
            }


            public List<BusinessOwner> GetAllBusinessOwners()
            {
                return _dbContext.BusinessOwners.ToList();
            }


            public BusinessOwner GetById(int id)
            {
                return _dbContext.BusinessOwners.FirstOrDefault(b => b.Id == id);
            }


            public void AddBusinessOwner(BusinessOwner businessOwner)
            {
                _dbContext.BusinessOwners.Add(businessOwner);
                _dbContext.SaveChanges();
            }


            public int UpdateBusinessOwner(BusinessOwner businessOwner)
            {
                _dbContext.BusinessOwners.Update(businessOwner);
                return _dbContext.SaveChanges();
            }


            public void DeleteBusinessOwner(int id)
            {
                var selectedBusinessOwner = _dbContext.BusinessOwners.FirstOrDefault(b => b.Id == id);
                if (selectedBusinessOwner != null)
                {
                    _dbContext.BusinessOwners.Remove(selectedBusinessOwner);
                    _dbContext.SaveChanges();
                }
            }


            public List<BusinessOwner> GetBusinessOwnersByType(string businessType)
            {
                return _dbContext.BusinessOwners
                    .Where(b => b.BusinessType == businessType)
                    .ToList();
            }


            public List<Rider> GetRidersForBusinessOwner(int businessOwnerId)
            {
                return _dbContext.Riders
                    .Where(r => r.BusinessID == businessOwnerId)
                    .ToList();
            }




            public void RemoveRiderFromBusinessOwner(int riderId)
            {
                var rider = _dbContext.Riders.FirstOrDefault(r => r.Id == riderId);
                if (rider != null)
                {
                    _dbContext.Riders.Remove(rider);
                    _dbContext.SaveChanges();
                }
            }






        }
    }

