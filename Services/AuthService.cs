using ADB2C_Implementation.Data;
using ADB2C_Implementation.DTOs;
using ADB2C_Implementation.Models;
using ADB2C_Implementation.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ADB2C_Implementation.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly ISession? _session;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _session = httpContextAccessor.HttpContext?.Session;
        }

        public async Task<string> GetAuthorizationUrlAsync()
        {
            try
            {
                var adb2cConfig = _configuration.GetSection("AzureAdB2C");
                var tenantName = adb2cConfig["Domain"]?.Split('.')[0];
                var policyId = adb2cConfig["SignUpSignInPolicyId"];
                var clientId = adb2cConfig["ClientId"];
                var redirectUri = adb2cConfig["RedirectUri"];
                var scope = adb2cConfig["Scope"];
                var responseType = adb2cConfig["ResponseType"];
                var responseMode = adb2cConfig["ResponseMode"];

                // Generate state parameter for security
                var state = Guid.NewGuid().ToString();
                SetState(state);

                // Build the ADB2C authorization URL
                var authUrl = $"https://{tenantName}.b2clogin.com/{tenantName}.onmicrosoft.com/{policyId}/oauth2/v2.0/authorize?" +
                    $"client_id={clientId}&" +
                    $"response_type={responseType}&" +
                    $"response_mode={responseMode}&" +
                    $"scope={scope}&" +
                    $"redirect_uri={Uri.EscapeDataString(redirectUri!)}&" +
                    $"state={state}";

                _logger.LogInformation("Generated ADB2C authorization URL");
                return authUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating authorization URL");
                throw;
            }
        }

        public async Task<TokenResponse> ExchangeCodeForTokensAsync(string code)
        {
            try
            {
                var adb2cConfig = _configuration.GetSection("AzureAdB2C");
                var tenantName = adb2cConfig["Domain"]?.Split('.')[0];
                var policyId = adb2cConfig["SignUpSignInPolicyId"];
                var clientId = adb2cConfig["ClientId"];
                var clientSecret = adb2cConfig["ClientSecret"];
                var redirectUri = adb2cConfig["RedirectUri"];

                var tokenEndpoint = $"https://{tenantName}.b2clogin.com/{tenantName}.onmicrosoft.com/{policyId}/oauth2/v2.0/token";

                var formData = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "authorization_code"),
                    new("client_id", clientId!),
                    new("client_secret", clientSecret!),
                    new("code", code),
                    new("redirect_uri", redirectUri!)
                };

                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(formData));

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Successfully exchanged code for tokens");
                    return JsonSerializer.Deserialize<TokenResponse>(json) ?? new TokenResponse();
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token exchange failed: {StatusCode} - {Content}. Token endpoint: {TokenEndpoint}",
                    response.StatusCode, errorContent, tokenEndpoint);
                _logger.LogError("Request parameters - ClientId: {ClientId}, RedirectUri: {RedirectUri}, PolicyId: {PolicyId}",
                    clientId, redirectUri, policyId);
                throw new Exception($"Token exchange failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging code for tokens");
                throw;
            }
        }

        public UserInfo DecodeIdToken(string? idToken)
        {
            try
            {
                if (string.IsNullOrEmpty(idToken))
                {
                    _logger.LogWarning("Id token is null or empty");
                    return new UserInfo();
                }

                // Extract the payload (middle part of JWT)
                var parts = idToken.Split('.');
                if (parts.Length < 2)
                {
                    _logger.LogError("Invalid JWT format in id_token");
                    return new UserInfo();
                }

                var base64Payload = parts[1];

                // Add padding if needed
                base64Payload = base64Payload.PadRight(base64Payload.Length + (4 - base64Payload.Length % 4) % 4, '=');

                // Decode
                var decodedBytes = Convert.FromBase64String(base64Payload);
                var jsonString = System.Text.Encoding.UTF8.GetString(decodedBytes);

                _logger.LogInformation("Decoded id_token successfully");

                // Parse the JSON
                var tokenData = JsonSerializer.Deserialize<JsonElement>(jsonString);

                var userInfo = new UserInfo
                {
                    Sub = tokenData.TryGetProperty("sub", out var sub) ? sub.GetString() ?? "" :
                          tokenData.TryGetProperty("oid", out var oid) ? oid.GetString() ?? "" : "",
                    Email = tokenData.TryGetProperty("email", out var email) ? email.GetString() :
                           tokenData.TryGetProperty("emails", out var emails) ? emails.EnumerateArray().FirstOrDefault().GetString() : null,
                    GivenName = tokenData.TryGetProperty("given_name", out var givenName) ? givenName.GetString() : null,
                    FamilyName = tokenData.TryGetProperty("family_name", out var familyName) ? familyName.GetString() : null
                };

                _logger.LogInformation("Extracted user info from id_token - Sub: {Sub}, Email: {Email}, GivenName: {GivenName}, FamilyName: {FamilyName}",
                    userInfo.Sub, userInfo.Email, userInfo.GivenName, userInfo.FamilyName);

                return userInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding id token");
                return new UserInfo();
            }
        }

        public async Task<User> SaveOrUpdateUserAsync(UserInfo userInfo, TokenResponse tokens)
        {
            try
            {
                _logger.LogInformation("Saving/updating user with ObjectId: {ObjectId}, Email: {Email}", userInfo.Sub, userInfo.Email);

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.ObjectId == userInfo.Sub);

                if (existingUser != null)
                {
                    _logger.LogInformation("Updating existing user with Id: {UserId}", existingUser.Id);

                    // Ensure PartitionKey is set and never changed
                    if (string.IsNullOrEmpty(existingUser.PartitionKey))
                    {
                        existingUser.PartitionKey = "User";
                    }

                    existingUser.Email = userInfo.Email;
                    existingUser.FirstName = userInfo.GivenName;
                    existingUser.LastName = userInfo.FamilyName;
                    existingUser.AccessToken = tokens.AccessToken;
                    existingUser.RefreshToken = tokens.RefreshToken;
                    existingUser.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn ?? 3600); // Default to 1 hour if not provided
                    existingUser.UpdatedAt = DateTime.UtcNow;

                    // Use Attach to ensure proper update tracking in Cosmos DB
                    var entry = _context.Entry(existingUser);
                    if (entry.State == EntityState.Detached)
                    {
                        _context.Attach(existingUser);
                    }
                    entry.State = EntityState.Modified;
                }
                else
                {
                    _logger.LogInformation("Creating new user with ObjectId: {ObjectId}", userInfo.Sub);
                    var newUser = new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        ObjectId = userInfo.Sub,
                        Email = userInfo.Email,
                        FirstName = userInfo.GivenName,
                        LastName = userInfo.FamilyName,
                        AccessToken = tokens.AccessToken,
                        RefreshToken = tokens.RefreshToken,
                        TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn ?? 3600),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        PartitionKey = "User"
                    };

                    _context.Users.Add(newUser);
                    existingUser = newUser;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully saved user with Id: {UserId}", existingUser.Id);
                return existingUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user to database. UserInfo.Sub: {Sub}, Email: {Email}", userInfo.Sub, userInfo.Email);
                throw;
            }
        }

        public bool ValidateState(string state)
        {
            var storedState = _session?.GetString("oauth_state");
            return state == storedState;
        }

        public void SetState(string state)
        {
            _session?.SetString("oauth_state", state);
        }
    }
}

