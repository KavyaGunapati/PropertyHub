namespace PropertyHub.Managers;
using PropertyHub.Services.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyHub.DataAccess.Context;
using PropertyHub.DataAccess.Entities;
using PropertyHub.Interfaces.IManager;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.JwtTokenService;
using PropertyHub.Models.DTOs;
using PropertyHub.DataAccess.Enums;

public class AppUserManager : IAppUserManager
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RegisterValidator _registerValidator;
    private readonly LoginValidator _loginValidator;
    private readonly ITokenService _tokenService;
    private readonly TokenConfiguration _tokenConfiguration;

    public AppUserManager(
        AppDbContext context,
        UserManager<User> userManager,
        RegisterValidator registerValidator,
        LoginValidator loginValidator,
        ITokenService tokenService,
        TokenConfiguration tokenConfiguration)
    {
        _context = context;
        _userManager = userManager;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _tokenService = tokenService;
        _tokenConfiguration = tokenConfiguration;
    }

    public async Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<RegisterResponse>.Failure(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (existingUser != null)
        {
            return Result<RegisterResponse>.Failure("Email is already registered.");
        }

        var user = new User
        {
            UserName = request.Name,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            Role = request.Role,
            IsActive = true
        };

        // UserManager hashes the password itself.
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<RegisterResponse>.Failure($"Failed to create user: {errors}");
        }

        return Result<RegisterResponse>.Success(new RegisterResponse
        {
            UserId = user.Id,
            Name = user.UserName,
            Email = user.Email
        });
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<LoginResponse>.Failure(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var user = await GetUserByEmailAsync(request.Email, cancellationToken);
        if (user == null || !user.IsActive || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Result<LoginResponse>.Failure("Invalid email or password.");
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenExpiration = _tokenConfiguration.RefreshTokenExpirationInMinutes,
            UserId = user.Id.ToString(),
            Name = user.UserName,
            Email = user.Email,
            Role = user.Role.ToString()
        });
    }
    public async Task<Result<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest refreshTokenRequest, CancellationToken cancellationToken = default)
    {
        var validateToken=await _tokenService.ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken, cancellationToken);
        if (validateToken!=TokenStatus.Valid)
        {
            return validateToken switch
            {
                TokenStatus.Expired=> Result<RefreshTokenResponse>.Failure("Refresh token has expired."),
                TokenStatus.Revoked=> Result<RefreshTokenResponse>.Failure("Refresh token has been revoked."),
                TokenStatus.InvalidSignature=> Result<RefreshTokenResponse>.Failure("Invalid refresh token."),
                _=> Result<RefreshTokenResponse>.Failure("Invalid refresh token.")
            };
        }
        var hashedToken=await _tokenService.GetRefreshTokenAsync(refreshTokenRequest.RefreshToken, cancellationToken);
        var refreshToken=await _context.RefreshTokens.FirstOrDefaultAsync(rt=>rt.Token==hashedToken.Token,cancellationToken);
        if(refreshToken==null || refreshToken.ExpiresAt<DateTime.UtcNow)
        {
            return Result<RefreshTokenResponse>.Failure("Invalid or expired refresh token.");
        }
        var user=await _context.Users.FirstOrDefaultAsync(u=>u.Id==refreshToken.UserId,cancellationToken);
        if(user==null || !user.IsActive)
        {
            return Result<RefreshTokenResponse>.Failure("User not found.");
        }
        var newAccessToken=_tokenService.GenerateAccessToken(user);
        var newRefreshToken=await _tokenService.GenerateRefreshTokenAsync(user.Id,cancellationToken);
        await _tokenService.RevokeRefreshTokenAsync(refreshTokenRequest.RefreshToken,cancellationToken);
        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse
        {
            AccessToken=newAccessToken,
            RefreshToken=newRefreshToken,
            Expiration=_tokenConfiguration.RefreshTokenExpirationInMinutes
        });

    }
    public async Task<Result<string>> LogoutAsync(string token, CancellationToken cancellationToken=default)
    {
        var validateToken=await _tokenService.ValidateRefreshTokenAsync(token, cancellationToken);
        if(validateToken!=TokenStatus.Valid)
        {
            return validateToken switch
            {
                TokenStatus.Expired=> Result<string>.Failure("Refresh token has expired."),
                TokenStatus.Revoked=> Result<string>.Failure("Refresh token has been revoked."),
                TokenStatus.InvalidSignature=> Result<string>.Failure("Invalid refresh token."),
                _=> Result<string>.Failure("Invalid refresh token.")
            };
        }
        var hashedToken=await _tokenService.GetRefreshTokenAsync(token, cancellationToken);
        var refreshTokenEntity=await _context.RefreshTokens.FirstOrDefaultAsync(rt=>rt.Token==hashedToken.Token,cancellationToken);
        if(refreshTokenEntity==null || refreshTokenEntity.ExpiresAt<DateTime.UtcNow)
        {
            return Result<string>.Failure("Invalid or expired refresh token.");
        }
        await _tokenService.RevokeRefreshTokenAsync(token, cancellationToken);
        return Result<string>.Success("Logout successful.");
    }
    public Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }
}
