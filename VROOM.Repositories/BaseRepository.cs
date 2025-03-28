using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public void CustomSaveChanges()
        {
            context.SaveChanges();
        }

        public async Task<T> GetAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }
        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }
        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }
    }

  
}
