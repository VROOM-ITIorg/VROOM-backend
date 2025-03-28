using System;
using System.Collections.Generic;
using System.Linq;
using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    public class PaymentManager
    {
        private readonly MyDbContext _dbContext;

        public PaymentManager(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public List<Payment> GetAllPayments() => _dbContext.Payments.Where(p => !p.IsDeleted).ToList();


        public Payment GetPaymentById(int id) => _dbContext.Payments
           .FirstOrDefault(p => p.Id == id && !p.IsDeleted);


        public void AddPayment(Payment payment)
        {
            _dbContext.Payments.Add(payment);
            _dbContext.SaveChanges();
        }

     
        public int UpdatePayment(Payment payment)
        {
            _dbContext.Payments.Update(payment);
            return _dbContext.SaveChanges();
        }

        
        public void DeletePayment(int id, string modifiedBy)
        {
            var payment = _dbContext.Payments.Find(id);
            if (payment != null)
            {
                payment.IsDeleted = true;
                payment.ModifiedBy = modifiedBy;
                payment.ModifiedAt = DateTime.UtcNow;
                _dbContext.SaveChanges();
            }
        }
    }
}
