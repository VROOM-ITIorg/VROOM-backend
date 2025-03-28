using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;

public class BusinessOwnerRepository : BaseRepository<BusinessOwner>
{
    private readonly MyDbContext context;
    public BusinessOwnerRepository(MyDbContext dbContext) : base(dbContext) {
    context = dbContext;
    }

    public List<BusinessOwner> GetAllBusinessOwners() => context.BusinessOwners.ToList();

    public BusinessOwner GetById(int id) => context.BusinessOwners.FirstOrDefault(b => b.Id == id);

    public void AddBusinessOwner(BusinessOwner businessOwner)
    {
        context.BusinessOwners.Add(businessOwner);
        CustomSaveChanges(); 
    }

    public void UpdateBusinessOwner(BusinessOwner businessOwner)
    {
        context.BusinessOwners.Update(businessOwner);
        CustomSaveChanges(); 
     
    }

    public void DeleteBusinessOwner(int id)
    {
        var selectedBusinessOwner = context.BusinessOwners.FirstOrDefault(b => b.Id == id);
        if (selectedBusinessOwner != null)
        {
            context.BusinessOwners.Remove(selectedBusinessOwner);
            CustomSaveChanges(); 
        }
    }

    public List<BusinessOwner> GetBusinessOwnersByType(string businessType) =>
        context.BusinessOwners.Where(b => b.BusinessType == businessType).ToList();
}
