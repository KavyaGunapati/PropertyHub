namespace PropertyHub.Services;
using FluentValidation;
using PropertyHub.Interfaces.IManager;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs;

public class AppUserService : IAppUserService
{
    private readonly IAppUserManager _manager;
    private readonly IValidator<RegisterRequest> _registerValidator;

    public AppUserService(IAppUserManager manager, IValidator<RegisterRequest> registerValidator)
    {
        _manager = manager;
        _registerValidator = registerValidator;
    }

    public async Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<RegisterResponse>.Failure(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        return await _manager.RegisterAsync(request, cancellationToken);
    }
}
