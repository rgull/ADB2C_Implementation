using ADB2C.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ADB2C.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Id is the ObjectId from ADB2C and serves as both primary key and partition key
                // This makes queries by ObjectId efficient (point reads on partition key)
                // Ensure the 'profiles' container partition key path is set to '/id'
                entity.HasPartitionKey(e => e.Id);
                entity.ToContainer("profiles");
            });
        }
    }
}

