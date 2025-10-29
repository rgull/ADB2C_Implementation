using ADB2C.Application.DTOs;
using ADB2C.Application.Interfaces;
using ADB2C.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ADB2C.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly ISession? _session;

        public AuthService(
            IUserRepository userRepository,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
            _session = httpContextAccessor.HttpContext?.Session;
        }

        public Task<string> GetAuthorizationUrlAsync()
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
                return Task.FromResult(authUrl);
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

                // ObjectId is now the Id, so we query by Id directly
                var existingUser = await _userRepository.GetByIdAsync(userInfo.Sub);

                if (existingUser != null)
                {
                    _logger.LogInformation("Updating existing user with Id: {UserId}", existingUser.Id);

                    existingUser.Email = userInfo.Email;
                    existingUser.FirstName = userInfo.GivenName;
                    existingUser.LastName = userInfo.FamilyName;
                    existingUser.AccessToken = tokens.AccessToken;
                    existingUser.RefreshToken = tokens.RefreshToken;
                    existingUser.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn ?? 3600); // Default to 1 hour if not provided
                    existingUser.UpdatedAt = DateTime.UtcNow;

                    return await _userRepository.UpdateAsync(existingUser);
                }
                else
                {
                    _logger.LogInformation("Creating new user with ObjectId: {ObjectId}", userInfo.Sub);
                    var newUser = new User
                    {
                        Id = userInfo.Sub, // ObjectId is now the Id
                        Email = userInfo.Email,
                        FirstName = userInfo.GivenName,
                        LastName = userInfo.FamilyName,
                        AccessToken = tokens.AccessToken,
                        RefreshToken = tokens.RefreshToken,
                        TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn ?? 3600),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    return await _userRepository.AddAsync(newUser);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user to database. UserInfo.Sub: {Sub}, Email: {Email}", userInfo.Sub, userInfo.Email);
                throw;
            }
        }

        public bool ValidateState(string state)
        {
            if (_session == null) return false;

            byte[]? stateBytes;
            if (_session.TryGetValue("oauth_state", out stateBytes) && stateBytes != null)
            {
                var storedState = Encoding.UTF8.GetString(stateBytes);
                return state == storedState;
            }
            return false;
        }

        public void SetState(string state)
        {
            if (_session != null)
            {
                var stateBytes = Encoding.UTF8.GetBytes(state);
                _session.Set("oauth_state", stateBytes);
            }
        }
    }
}

