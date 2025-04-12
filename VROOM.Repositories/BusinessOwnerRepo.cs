using VROOM.Data;
using VROOM.Models;

/*
0- CRUD 
1- Get Business Owners by Business Type
2- Get Riders for a Business Owner
3- Assign a Rider to a Business Owner 
4- Remove a Rider from a Business Owner
5- Pagination //not yet
6- edge cases //not yet
 
 */


namespace VROOM.Repositories
{

    public class BusinessOwnerManager
    {
        private readonly MyDbContext _dbContext;
        public BusinessOwnerManager(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public List<BusinessOwner> GetAllBusinessOwners() => _dbContext.BusinessOwners.ToList();


        public BusinessOwner GetById(int id) => _dbContext.BusinessOwners.FirstOrDefault(b => b.Id == id);


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


        public List<BusinessOwner> GetBusinessOwnersByType(string businessType) => _dbContext.BusinessOwners
                    .Where(b => b.BusinessType == businessType)
                    .ToList();


        public List<Rider> GetRidersForBusinessOwner(int businessOwnerId) => _dbContext.Riders
                    .Where(r => r.BusinessID == businessOwnerId)
                    .ToList();

        public void AssignRiderToBusinessOwner(int businessOwnerId, int riderId)
        {
            _dbContext.RiderAssignments.Add(new RiderAssignment
            {
                BusinessID = businessOwnerId,
                RiderID = riderId,
                AssignmentDate = DateTime.UtcNow
            });

            _dbContext.SaveChanges();
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

