namespace PropertyHub.Managers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyHub.Interfaces.IManager;
using PropertyHub.Models.DTOs;
using PropertyHub.DataAccess.Context;
using PropertyHub.DataAccess.Entities;

public class AppUserManager : IAppUserManager
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public AppUserManager(AppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
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
}
