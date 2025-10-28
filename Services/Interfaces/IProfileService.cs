using ADB2C_Implementation.DTOs;
using ADB2C_Implementation.Models;

namespace ADB2C_Implementation.Services.Interfaces
{
    public interface IProfileService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(string userId);
        Task<User> UpdateUserAsync(string userId, UpdateUserRequest request);
    }
}

