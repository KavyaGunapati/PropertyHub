namespace PropertyHub.Services;
using PropertyHub.DataAccess.Entities;
using PropertyHub.Interfaces.IManager;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs;

public class AppUserService : IAppUserService
{
    private readonly IAppUserManager _manager;

    public AppUserService(IAppUserManager manager)
    {
        _manager = manager;
    }

    public Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        => _manager.RegisterAsync(request, cancellationToken);

    public Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        => _manager.LoginAsync(request, cancellationToken);
    public Task<Result<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest refreshTokenRequest, CancellationToken cancellationToken = default)
        => _manager.RefreshTokenAsync(refreshTokenRequest, cancellationToken);
    public Task<Result<string>> LogoutAsync(string token, CancellationToken cancellationToken = default)
        => _manager.LogoutAsync(token, cancellationToken);

    public Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _manager.GetUserByEmailAsync(email, cancellationToken);
}
