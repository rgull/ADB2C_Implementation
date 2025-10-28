using ADB2C_Implementation.Data;
using ADB2C_Implementation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ADB2C_Implementation.Controllers
{
    [ApiController]
    [Route("oauth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("adb2c/login")]
        public IActionResult Login()
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
                HttpContext.Session.SetString("oauth_state", state);

                // Build the ADB2C authorization URL
                var authUrl = $"https://{tenantName}.b2clogin.com/{tenantName}.onmicrosoft.com/{policyId}/oauth2/v2.0/authorize?" +
                    $"client_id={clientId}&" +
                    $"response_type={responseType}&" +
                    $"response_mode={responseMode}&" +
                    $"scope={scope}&" +
                    $"redirect_uri={Uri.EscapeDataString(redirectUri!)}&" +
                    $"state={state}";

                _logger.LogInformation("Redirecting to ADB2C login: {AuthUrl}", authUrl);

                return Redirect(authUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating ADB2C login");
                return BadRequest("Error initiating login process");
            }
        }


        [HttpGet("adb2c-redirect")]
        public async Task<IActionResult> Redirect()
        {
            try
            {
                var code = Request.Query["code"].FirstOrDefault();
                var state = Request.Query["state"].FirstOrDefault();
                var error = Request.Query["error"].FirstOrDefault();

                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogError("ADB2C returned error: {Error}", error);
                    return BadRequest($"Authentication error: {error}");
                }

                if (string.IsNullOrEmpty(code))
                {
                    _logger.LogError("No authorization code received from ADB2C");
                    return BadRequest("No authorization code received");
                }

                // Verify state parameter
                var storedState = HttpContext.Session.GetString("oauth_state");
                if (state != storedState)
                {
                    _logger.LogError("Invalid state parameter");
                    return BadRequest("Invalid state parameter");
                }

                // Exchange code for tokens
                var tokens = await ExchangeCodeForTokens(code);
                if (tokens == null)
                {
                    return BadRequest("Failed to exchange code for tokens");
                }

                // Decode user info from id_token
                var userInfo = DecodeIdToken(tokens.AccessToken);
                
                if (userInfo == null || string.IsNullOrEmpty(userInfo.Sub))
                {
                    _logger.LogError("Failed to decode id_token and no user information available");
                    return BadRequest("Failed to get user information from id_token");
                }

                // Save or update user in database
                var user = await SaveOrUpdateUser(userInfo, tokens);

                // Redirect to React frontend with success
                var frontendRedirectUrl = _configuration["AzureAdB2C:FrontendRedirectUrl"];
                return Redirect($"{frontendRedirectUrl}?userId={user.Id}&token={tokens.AccessToken}&success=true");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ADB2C redirect");
                var frontendRedirectUrl = _configuration["AzureAdB2C:FrontendRedirectUrl"];
                return Redirect($"{frontendRedirectUrl}?error={ex.Message}&success=false");
            }
        }

        private async Task<TokenResponse?> ExchangeCodeForTokens(string code)
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
                    return JsonSerializer.Deserialize<TokenResponse>(json);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token exchange failed: {StatusCode} - {Content}. Token endpoint: {TokenEndpoint}", 
                    response.StatusCode, errorContent, tokenEndpoint);
                _logger.LogError("Request parameters - ClientId: {ClientId}, RedirectUri: {RedirectUri}, PolicyId: {PolicyId}", 
                    clientId, redirectUri, policyId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging code for tokens");
                return null;
            }
        }

        private UserInfo? DecodeIdToken(string? idToken)
        {
            try
            {
                if (string.IsNullOrEmpty(idToken))
                {
                    _logger.LogWarning("Id token is null or empty");
                    return null;
                }

                // Extract the payload (middle part of JWT)
                var parts = idToken.Split('.');
                if (parts.Length < 2)
                {
                    _logger.LogError("Invalid JWT format in id_token");
                    return null;
                }

                var base64Payload = parts[1];
                
                // Add padding if needed
                base64Payload = base64Payload.PadRight(base64Payload.Length + (4 - base64Payload.Length % 4) % 4, '=');

                // Decode
                var decodedBytes = Convert.FromBase64String(base64Payload);
                var jsonString = System.Text.Encoding.UTF8.GetString(decodedBytes);
                
                _logger.LogInformation("Decoded id_token: {Json}", jsonString);
                
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
                return null;
            }
        }
        private async Task<User> SaveOrUpdateUser(UserInfo userInfo, TokenResponse tokens)
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
                throw; // Re-throw to be caught by the caller
            }
        }
    }

    public class TokenResponse
    {
        [JsonPropertyName("id_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }

    public class UserInfo
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; set; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }
    }
}
