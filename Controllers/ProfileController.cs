using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ADB2C_Implementation.Models;
using ADB2C_Implementation.Services.Interfaces;

namespace ADB2C_Implementation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(IProfileService profileService, ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            try
            {
                var users = await _profileService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return BadRequest("Error retrieving users");
            }
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<User>> GetUser(string userId)
        {
            try
            {
                var user = await _profileService.GetUserByIdAsync(userId);
                
                if (user == null)
                {
                    return NotFound("User not found");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return BadRequest("Error retrieving user profile");
            }
        }

        [HttpPut("{userId}")]
        public async Task<ActionResult<User>> UpdateUser(string userId, [FromBody] DTOs.UpdateUserRequest request)
        {
            try
            {
                var user = await _profileService.UpdateUserAsync(userId, request);
                return Ok(user);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("User not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile. UserId: {UserId}", userId);
                return BadRequest($"Error updating user profile: {ex.Message}");
            }
        }
    }
}
