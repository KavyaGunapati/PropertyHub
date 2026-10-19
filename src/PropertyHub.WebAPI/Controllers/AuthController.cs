namespace PropertyHub.WebAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAppUserService _userService;

    public AuthController(IAppUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.RegisterAsync(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = "Registration successful", data = result.Data });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.LoginAsync(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = "Login successful", data = result.Data });
    }
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.RefreshTokenAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { message = "Token refreshed successfully", data = result.Data });
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(string token, CancellationToken cancellationToken)
    {
        var result = await _userService.LogoutAsync(token, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { message = "Logged out successfully", data = result.Data });
    }
}
