using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ADB2C.Infrastructure.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeDatabase(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

            try
            {
                logger.LogInformation("Initializing Cosmos DB database and container...");
                // Ensure the database and container are created
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("Cosmos DB database and container initialized successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error initializing Cosmos DB: {ErrorMessage}", ex.Message);
                throw;
            }
        }
    }
}

