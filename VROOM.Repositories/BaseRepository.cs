using Microsoft.EntityFrameworkCore;
using VROOM.Data;

namespace VROOM.Repositories
{
    public class BaseRepository<T> where T : class
    {

        private readonly MyDbContext context;
        private readonly DbSet<T> _dbSet;
        public BaseRepository(MyDbContext context)
        {
            context = context;
            _dbSet = context.Set<T>();
        }

        public virtual void CustomSaveChanges()
        {
            context.SaveChanges();
        }

        public virtual async Task<T> GetAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }
        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }
        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
        }
        public virtual void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }
    }


}
