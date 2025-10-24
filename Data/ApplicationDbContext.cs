using Microsoft.EntityFrameworkCore;
using ADB2C_Implementation.Models;

namespace ADB2C_Implementation.Data
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
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.ToContainer("Users");
                
                // Configure unique constraints
                entity.HasIndex(e => e.ObjectId).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });
        }
    }
}
