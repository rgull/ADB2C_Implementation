using Microsoft.EntityFrameworkCore;
using ADB2C_Implementation.Data;

namespace ADB2C_Implementation
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeDatabase(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            try
            {
                // Ensure the database and container are created
                await context.Database.EnsureCreatedAsync();
                Console.WriteLine("Cosmos DB database and container initialized successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Cosmos DB: {ex.Message}");
                throw;
            }
        }
    }
}
