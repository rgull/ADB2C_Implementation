using ADB2C.Application.DTOs;
using ADB2C.Domain.Entities;

namespace ADB2C.Application.Interfaces
{
    public interface IProfileService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(string userId);
        Task<User> UpdateUserAsync(string userId, UpdateUserRequest request);
    }
}

