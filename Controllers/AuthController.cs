using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ADB2C_Implementation.Data;
using ADB2C_Implementation.Models;
using System.Security.Claims;
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

                // Get user info from ADB2C
                var userInfo = await GetUserInfo(tokens.AccessToken);
                if (userInfo == null)
                {
                    return BadRequest("Failed to get user information");
                }

                // Save or update user in database
                var user = await SaveOrUpdateUser(userInfo, tokens);

                // Redirect to React frontend with success
                return Redirect($"http://localhost:3000?userId={user.Id}&success=true");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ADB2C redirect");
                return BadRequest("Error processing authentication");
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

        private async Task<UserInfo?> GetUserInfo(string accessToken)
        {
            try
            {
                var adb2cConfig = _configuration.GetSection("AzureAdB2C");
                var tenantName = adb2cConfig["Domain"]?.Split('.')[0];
                var policyId = adb2cConfig["SignUpSignInPolicyId"];

                var userInfoEndpoint = $"https://{tenantName}.b2clogin.com/{tenantName}.onmicrosoft.com/{policyId}/openid/v2.0/userinfo";

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.GetAsync(userInfoEndpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserInfo>(json);
                }

                _logger.LogError("User info request failed: {StatusCode} - {Content}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info");
                return null;
            }
        }

        private async Task<User> SaveOrUpdateUser(UserInfo userInfo, TokenResponse tokens)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.ObjectId == userInfo.Sub);

            if (existingUser != null)
            {
                // Update existing user
                existingUser.Email = userInfo.Email;
                existingUser.FirstName = userInfo.GivenName;
                existingUser.LastName = userInfo.FamilyName;
                existingUser.AccessToken = tokens.AccessToken;
                existingUser.RefreshToken = tokens.RefreshToken;
                existingUser.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn);
                existingUser.UpdatedAt = DateTime.UtcNow;

                _context.Users.Update(existingUser);
            }
            else
            {
                // Create new user
                var newUser = new User
                {
                    ObjectId = userInfo.Sub,
                    Email = userInfo.Email,
                    FirstName = userInfo.GivenName,
                    LastName = userInfo.FamilyName,
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                existingUser = newUser;
            }

            await _context.SaveChangesAsync();
            return existingUser;
        }
    }

    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
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
