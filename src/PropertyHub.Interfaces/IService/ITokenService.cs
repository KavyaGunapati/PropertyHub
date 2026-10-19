using PropertyHub.DataAccess.Entities;
using PropertyHub.DataAccess.Enums;
namespace PropertyHub.Interfaces.IService
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        Task<string> GenerateRefreshTokenAsync(int userId, CancellationToken cancellationToken);
        Task<RefreshToken> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
        Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
        Task<TokenStatus> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
        
    }
}