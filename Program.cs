
using ADB2C_Implementation;
using ADB2C_Implementation.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

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
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "ADB2C Implementation API", 
                    Version = "v1" 
                });
                
                // Add JWT Bearer authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Configure Azure AD B2C JWT Authentication
            var adb2cConfig = builder.Configuration.GetSection("AzureAdB2C");
            var tenantName = adb2cConfig["Domain"]?.Split('.')[0];
            var policyId = adb2cConfig["SignUpSignInPolicyId"];
            
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = $"https://{tenantName}.b2clogin.com/{tenantName}.onmicrosoft.com/{policyId}/v2.0/";
                options.MetadataAddress = $"https://{tenantName}.b2clogin.com/{tenantName}.onmicrosoft.com/{policyId}/v2.0/.well-known/openid-configuration";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = $"https://{tenantName}.b2clogin.com/{tenantName}.onmicrosoft.com/{policyId}/v2.0/",
                    ValidAudiences = new[] { adb2cConfig["ClientId"]! }
                };
            });

            builder.Services.AddAuthorization();

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
            app.UseAuthentication();
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
