using System.Text.Json.Serialization;

namespace ADB2C.Application.DTOs
{
    public class TokenResponse
    {
        [JsonPropertyName("id_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }
}

