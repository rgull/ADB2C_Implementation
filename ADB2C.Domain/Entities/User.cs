namespace ADB2C.Domain.Entities
{
    public class User
    {
        // Id is the ObjectId from ADB2C and serves as both primary key and partition key in Cosmos DB
        public string Id { get; set; } = string.Empty;

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
    }
}

