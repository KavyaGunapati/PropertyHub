using PropertyHub.Models.DTOs;
namespace PropertyHub.Interfaces.IService
{
    public interface IFavoriteService
    {
        Task<Result<string>> AddAsync(int userId, int propertyId, CancellationToken cancellationToken = default);
        Task<Result<string>> RemoveAsync(int userId, int propertyId, CancellationToken cancellationToken = default);
        Task<Result<List<FavoriteResponse>>> GetMyFavoritesAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}