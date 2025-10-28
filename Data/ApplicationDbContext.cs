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
                // Use Id as the partition key to match existing Cosmos DB container
                // Note: The existing 'profiles' container was created with partition key path 'id'
                entity.HasPartitionKey(e => e.Id);
                entity.ToContainer("profiles");
                
                // The PartitionKey property in the User model is just metadata
                // The actual partition key for Cosmos DB operations is the Id field
            });
        }
    }
}
