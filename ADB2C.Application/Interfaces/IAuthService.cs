using ADB2C.Application.DTOs;
using ADB2C.Domain.Entities;

namespace ADB2C.Application.Interfaces
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

