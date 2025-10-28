using ADB2C_Implementation.DTOs;
using ADB2C_Implementation.Models;

namespace ADB2C_Implementation.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string> GetAuthorizationUrlAsync();
        Task<TokenResponse> ExchangeCodeForTokensAsync(string code);
        UserInfo DecodeIdToken(string? idToken);
        Task<User> SaveOrUpdateUserAsync(UserInfo userInfo, TokenResponse tokens);
        bool ValidateState(string state);
        void SetState(string state);
    }
}

