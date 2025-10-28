using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ADB2C_Implementation.Data;
using ADB2C_Implementation.Models;

namespace ADB2C_Implementation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(ApplicationDbContext context, ILogger<ProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
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
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);
                
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
        public async Task<ActionResult<User>> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);
                
                if (user == null)
                {
                    return NotFound("User not found");
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

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile. UserId: {UserId}", userId);
                return BadRequest($"Error updating user profile: {ex.Message}");
            }
        }
    }

    public class UpdateUserRequest
    {
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Number { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
    }
}
