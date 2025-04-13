using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VROOM.Data;

namespace VROOM.Repositories
{
    public class BaseRepository<T> where T : class
    {

        protected readonly VroomDbContext context;
        protected readonly DbSet<T> dbSet;
        public BaseRepository(VroomDbContext _context)
        {
            context = _context;
            dbSet = context.Set<T>();
        }

        public void CustomSaveChanges()
        {
            context.SaveChanges();
        }

        public async Task<T> GetAsync(int id)
        {
            return await dbSet.FindAsync(id);
        }
        public async Task<T> GetAsync(string id)
        {
            return await dbSet.FindAsync(id);
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await dbSet.ToListAsync();
        }
        public IQueryable<T> GetList(Expression<Func<T, bool>> Filter = null)
        {
            if (Filter == null) return dbSet.AsQueryable();
            else return dbSet.Where(Filter);
        }
        public void Add(T entity)
        {
             dbSet.Add(entity);
        }
        public void Update(T entity)
        {
            dbSet.Update(entity);
        }
        public void Delete(T entity)
        {
            dbSet.Remove(entity);
        }
    }


}
