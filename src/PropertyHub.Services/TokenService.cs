using PropertyHub.Interfaces.IService;
using PropertyHub.DataAccess.Entities;
using PropertyHub.DataAccess.Enums;
using PropertyHub.DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace PropertyHub.Models.JwtTokenService
{
    public class TokenService:ITokenService
    {
        private readonly TokenConfiguration _tokenConfiguration;
        private readonly AppDbContext _dbContext;
        public TokenService(TokenConfiguration tokenConfiguration, AppDbContext dbContext)
        {
            _tokenConfiguration = tokenConfiguration;
            _dbContext = dbContext;
        }
        public string GenerateAccessToken(User user)
        {
            var key =new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenConfiguration.AccessTokenSecret));
            var cred=new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };
            var token=new JwtSecurityToken(
                issuer: _tokenConfiguration.Issuer,
                audience: _tokenConfiguration.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_tokenConfiguration.AccessTokenExpirationInMinutes),
                signingCredentials: cred
            );
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
        }
        public async Task<string> GenerateRefreshTokenAsync(int userId,CancellationToken cancellationToken)
        {
            var refreshTokenString = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
            var hashedToken = HashToken(refreshTokenString);
            var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = hashedToken,
            JwtId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_tokenConfiguration.RefreshTokenExpirationInMinutes / 1440.0)
        };
            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return refreshTokenString; // raw token goes to the client; only the hash is stored
        }
        public async Task<RefreshToken> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var hashedToken=HashToken(refreshToken);
            return await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == hashedToken, cancellationToken);
        }
        public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var hashedToken=HashToken(refreshToken);
            var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == hashedToken, cancellationToken);
            if(token != null)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokeReason = "Revoked by user or system";
                await _dbContext.SaveChangesAsync(cancellationToken);

            }
        }
        public async Task<TokenStatus> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var hasedToken=HashToken(refreshToken);
            var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == hasedToken, cancellationToken);
            if(token == null)
            {
                return TokenStatus.InvalidSignature;
            }
            if(token.IsRevoked)
            {
                return TokenStatus.Revoked;
            }
            if(token.IsExpired)
            {
                return TokenStatus.Expired;
            }
            return TokenStatus.Valid;
        }
        private string HashToken(string token)
        {
            using(var sha256=System.Security.Cryptography.SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(token);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}