namespace PropertyHub.Interfaces.IManager;
using PropertyHub.Models.DTOs;

public interface IAppUserManager
{
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
