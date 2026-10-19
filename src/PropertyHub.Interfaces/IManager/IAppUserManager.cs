namespace PropertyHub.Interfaces.IManager;
using PropertyHub.Models.DTOs;
using PropertyHub.DataAccess.Entities;

public interface IAppUserManager
{
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest refreshTokenRequest,CancellationToken cancellationToken=default);
    Task<Result<string>> LogoutAsync(string token, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
}
