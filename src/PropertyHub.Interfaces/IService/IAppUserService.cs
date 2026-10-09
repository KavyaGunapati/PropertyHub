namespace PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs;

public interface IAppUserService
{
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
