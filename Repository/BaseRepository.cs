using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VROOM.Data;

namespace VROOM.Repositories
{
    public class BaseRepository<T> where T : class
    {
        protected readonly VroomDbContext context;
        protected readonly DbSet<T> dbSet;

        public BaseRepository(VroomDbContext _context)
        {
            context = _context ?? throw new ArgumentNullException(nameof(_context));
            dbSet = context.Set<T>();
        }

        public IQueryable<T> GetLocal()
        {
            return context.Set<T>().Local.AsQueryable();
        }

        public T GetLocal(Expression<Func<T, bool>> filter)
        {
            return context.Set<T>().Local.AsQueryable().FirstOrDefault(filter);
        }

        public async Task<T> GetLocalOrDbAsync(
            Expression<Func<T, bool>> filter,
            bool useNoTracking = false,
            params Expression<Func<T, object>>[] includes)
        {
            var localQuery = context.Set<T>().Local.AsQueryable();
            if (filter != null)
            {
                localQuery = localQuery.Where(filter);
            }

            var localEntity = localQuery.FirstOrDefault();
            if (localEntity != null)
            {
                return localEntity;
            }

            IQueryable<T> query = dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            if (useNoTracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<T>> GetListLocalOrDbAsync(
            Expression<Func<T, bool>> filter = null,
            bool useNoTracking = false,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> localQuery = context.Set<T>().Local.AsQueryable();
            if (filter != null)
            {
                localQuery = localQuery.Where(filter);
            }

            var localEntities = localQuery.ToList();
            if (localEntities.Any())
            {
                return localEntities;
            }

            IQueryable<T> query = dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            if (useNoTracking)
            {
                query = query.AsNoTracking();
            }

            return await query.ToListAsync();
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

        public IQueryable<T> GetList(Expression<Func<T, bool>> filter = null)
        {
            return filter != null ? dbSet.Where(filter) : dbSet.AsQueryable();
        }

        public IQueryable<T> Get(
            Expression<Func<T, bool>> filter = null,
            int pageSize = 4,
            int pageNumber = 1)
        {
            IQueryable<T> query = dbSet.AsQueryable();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (pageSize < 0)
            {
                pageSize = 4;
            }

            if (pageNumber < 0)
            {
                pageNumber = 1;
            }

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

        public void CustomSaveChanges()
        {
            context.SaveChanges();
        }

        public async Task CustomSaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }
    }
}