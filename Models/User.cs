using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADB2C_Implementation.Models
{
    public class User
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string ObjectId { get; set; } = string.Empty; // UUID from ADB2C
        
        public string? Username { get; set; }
        
        public string? Email { get; set; }
        
        public string? FirstName { get; set; }
        
        public string? LastName { get; set; }
        
        public string? Number { get; set; }
        
        public string? Gender { get; set; }
        
        public string? Address { get; set; }
        
        public string? AccessToken { get; set; }
        
        public string? RefreshToken { get; set; }
        
        public DateTime TokenExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Cosmos DB partition key - must be set and never changed
        [Required]
        public string PartitionKey { get; set; } = "User";
    }
}
