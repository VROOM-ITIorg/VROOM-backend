using Microsoft.EntityFrameworkCore.Storage;
using VROOM.Data;

namespace VROOM.Repositories
{
    public class TransactionWork<T> where T : class
    {
        private readonly VroomDbContext context;
        private IDbContextTransaction transaction;
        public BaseRepository<T> User;

        public TransactionWork(VroomDbContext _context, BaseRepository<T> _User) { 
            context = _context;
            User = _User;
        }

        public async Task BeginTransactionAsync()
            
        {
            transaction = await context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
             transaction.CommitAsync();
        }

        public async Task RollbackAsync()
        {
            await transaction.RollbackAsync();
        }


    }
}
