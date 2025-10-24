
using ADB2C_Implementation;
using ADB2C_Implementation.Data;
using Microsoft.EntityFrameworkCore;

namespace ADB2C_Implementation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseCosmos(
                    builder.Configuration.GetConnectionString("CosmosDb:AccountEndpoint")!,
                    builder.Configuration.GetConnectionString("CosmosDb:AccountKey")!,
                    builder.Configuration.GetConnectionString("CosmosDb:DatabaseName")!));

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add session support for state management
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add CORS for React frontend
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ReactApp", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("ReactApp");
            app.UseSession();
            app.UseAuthorization();

            app.MapControllers();

            // Initialize Cosmos DB
            using (var scope = app.Services.CreateScope())
            {
                 DatabaseInitializer.InitializeDatabase(scope.ServiceProvider);
            }

            app.Run();
        }
    }
}
