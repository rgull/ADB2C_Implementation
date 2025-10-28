using ADB2C_Implementation.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ADB2C_Implementation.Controllers
{
    [ApiController]
    [Route("oauth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _authService = authService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("adb2c/login")]
        public async Task<IActionResult> Login()
        {
            try
            {
                var authUrl = await _authService.GetAuthorizationUrlAsync();
                _logger.LogInformation("Redirecting to ADB2C login");
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
                if (!_authService.ValidateState(state!))
                {
                    _logger.LogError("Invalid state parameter");
                    return BadRequest("Invalid state parameter");
                }

                // Exchange code for tokens
                var tokens = await _authService.ExchangeCodeForTokensAsync(code);
                if (tokens == null)
                {
                    return BadRequest("Failed to exchange code for tokens");
                }

                // Decode user info from id_token
                var userInfo = _authService.DecodeIdToken(tokens.AccessToken);
                
                if (userInfo == null || string.IsNullOrEmpty(userInfo.Sub))
                {
                    _logger.LogError("Failed to decode id_token and no user information available");
                    return BadRequest("Failed to get user information from id_token");
                }

                // Save or update user in database
                var user = await _authService.SaveOrUpdateUserAsync(userInfo, tokens);

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

    }
}
