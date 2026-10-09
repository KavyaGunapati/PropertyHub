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
}
