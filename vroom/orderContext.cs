using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vroom.Models;

namespace vroom
{
    public class OrderContext : DbContext
    {
        public DbSet<Order> orders;
        public DbSet<OrderPriority> priority;
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data source = ROZIE\\MSSQLSERVER01 ; Initial catalog = Vroom; Integrated security= true; trustservercertificate = true;");
            base.OnConfiguring(optionsBuilder);
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new OrderConfig());
            modelBuilder.ApplyConfiguration(new OrderPriorityConfig());
  

            base.OnModelCreating(modelBuilder);
        }
    }
}
