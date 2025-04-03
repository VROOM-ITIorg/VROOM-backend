using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VROOM.Data;

namespace VROOM.Repositories
{
    public class BaseManager<T> where T : class
    {

        protected readonly VroomDbContext dbcontext;
        protected readonly DbSet<T> table;
        public BaseManager(VroomDbContext _deliverySys)
        {
            dbcontext = _deliverySys;
            table = dbcontext.Set<T>();
        }


        public IQueryable<T> Get(
            Expression<Func<T, bool>> filter = null,
            int pageSize = 4,
            int pageNumber = 1)
        {
            IQueryable<T> quary = table.AsQueryable();

            if (filter != null)
                quary = quary.Where(filter);

            //Pagination
            if (pageSize < 0)
                pageSize = 4;

            if (pageNumber < 0)
                pageNumber = 1;

            int count = quary.Count();

            if (count < pageSize)
            {
                pageSize = count;
                pageNumber = 1;
            }

            int ToSkip = (pageNumber - 1) * pageSize;

            quary = quary.Skip(ToSkip).Take(pageSize);

            return quary;
        }

        public IQueryable<T> GetList(
            Expression<Func<T, bool>> filter = null)
        {
            return filter == null ? table.AsQueryable() : table.Where(filter);
        }
        public void Add(T newRow)
        {
            table.Add(newRow);
            dbcontext.SaveChanges();
        }
        public void Edit(T newRow)
        {
            table.Update(newRow);
            dbcontext.SaveChanges();
        }

        public void Delete(T row)
        {
            table.Remove(row);
            dbcontext.SaveChanges();
        }
    }
}
