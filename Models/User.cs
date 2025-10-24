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
        
        public string? Email { get; set; }
        
        public string? FirstName { get; set; }
        
        public string? LastName { get; set; }
        
        public string? AccessToken { get; set; }
        
        public string? RefreshToken { get; set; }
        
        public DateTime TokenExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Cosmos DB partition key
        public string PartitionKey { get; set; } = "User";
    }
}
