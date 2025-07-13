// VROOM.Repositories/BaseRepository.cs
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

        public IQueryable<T> GetList(Expression<Func<T, bool>> filter = null)
        {
            return filter != null ? dbSet.Where(filter) : dbSet;
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

        public IQueryable<T> Get(
            Expression<Func<T, bool>> filter = null,
            int pageSize = 4,
            int pageNumber = 1)
        {
            IQueryable<T> query = dbSet.AsQueryable();

            if (filter != null)
                query = query.Where(filter);

            // Pagination
            if (pageSize < 0)
                pageSize = 4;

            if (pageNumber < 0)
                pageNumber = 1;

            int count = query.Count();

            if (count < pageSize)
            {
                pageSize = count;
                pageNumber = 1;
            }

            int toSkip = (pageNumber - 1) * pageSize;

            query = query.Skip(toSkip).Take(pageSize);

            return query;
        }
    }
}