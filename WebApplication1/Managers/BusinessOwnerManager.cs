using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Data;
using YourNamespace.Models;

namespace AdminArea.Managers
{
    public class BusinessOwnerManager
    {
        private readonly MyDbContext _dbContext;
        public DbSet<BusinessOwner> table;
        public BusinessOwnerManager(MyDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public  List<BusinessOwner> GetAllBusinessOwners()
        {
            return  _dbContext.BusinessOwners.ToList();
        }

        public  BusinessOwner GetById(int id)
        {
            return  _dbContext.BusinessOwners.FirstOrDefault(b => b.BusinessID == id);

        }

        public void AddBusinessOwner(BusinessOwner businessOwner)
        {
            _dbContext.BusinessOwners.Add(businessOwner);
             _dbContext.SaveChanges();
        }
        public int UpdateBusinessOwner(BusinessOwner businessOwner)
        {
            _dbContext.BusinessOwners.Update(businessOwner);
            return _dbContext.SaveChanges(); // Returns the number of affected rows
        }


        public void DeleteBusinessOwner(int id)
        {
           var selectedBusinessOwner = _dbContext.BusinessOwners.FirstOrDefault(b => b.BusinessID == id);
            if (selectedBusinessOwner != null)
            {
                _dbContext.BusinessOwners.Remove(selectedBusinessOwner);
                _dbContext.SaveChanges();
            }
            }
    }
}
