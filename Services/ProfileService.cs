using ADB2C_Implementation.Data;
using ADB2C_Implementation.DTOs;
using ADB2C_Implementation.Models;
using ADB2C_Implementation.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADB2C_Implementation.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(ApplicationDbContext context, ILogger<ProfileService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all users");
                var users = await _context.Users.ToListAsync();
                _logger.LogInformation("Retrieved {Count} users", users.Count);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Retrieving user with Id: {UserId}", userId);
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found with Id: {UserId}", userId);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile for Id: {UserId}", userId);
                throw;
            }
        }

        public async Task<User> UpdateUserAsync(string userId, UpdateUserRequest request)
        {
            try
            {
                _logger.LogInformation("Updating user with Id: {UserId}", userId);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found for update. Id: {UserId}", userId);
                    throw new KeyNotFoundException($"User with Id {userId} not found");
                }

                // Ensure PartitionKey is set (it should already be set, but this prevents issues)
                if (string.IsNullOrEmpty(user.PartitionKey))
                {
                    user.PartitionKey = "User";
                }

                // Update user fields
                if (!string.IsNullOrEmpty(request.Username))
                    user.Username = request.Username;

                if (!string.IsNullOrEmpty(request.FirstName))
                    user.FirstName = request.FirstName;

                if (!string.IsNullOrEmpty(request.LastName))
                    user.LastName = request.LastName;

                if (!string.IsNullOrEmpty(request.Email))
                    user.Email = request.Email;

                if (!string.IsNullOrEmpty(request.Number))
                    user.Number = request.Number;

                if (!string.IsNullOrEmpty(request.Gender))
                    user.Gender = request.Gender;

                if (!string.IsNullOrEmpty(request.Address))
                    user.Address = request.Address;

                user.UpdatedAt = DateTime.UtcNow;

                // Use AsNoTracking and reattach to ensure proper update tracking
                // This prevents partition key issues with Cosmos DB
                var entry = _context.Entry(user);
                if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
                {
                    _context.Attach(user);
                }
                entry.State = Microsoft.EntityFrameworkCore.EntityState.Modified;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated user with Id: {UserId}", userId);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile. UserId: {UserId}", userId);
                throw;
            }
        }
    }
}

