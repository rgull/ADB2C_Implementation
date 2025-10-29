using ADB2C.Application.DTOs;
using ADB2C.Application.Interfaces;
using ADB2C.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ADB2C.Infrastructure.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(IUserRepository userRepository, ILogger<ProfileService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all users");
                var users = await _userRepository.GetAllAsync();
                _logger.LogInformation("Retrieved {Count} users", users.Count());
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
                var user = await _userRepository.GetByIdAsync(userId);

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

                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found for update. Id: {UserId}", userId);
                    throw new KeyNotFoundException($"User with Id {userId} not found");
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

                return await _userRepository.UpdateAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile. UserId: {UserId}", userId);
                throw;
            }
        }
    }
}

