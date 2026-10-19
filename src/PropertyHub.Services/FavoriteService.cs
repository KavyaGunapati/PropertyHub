using PropertyHub.DataAccess.Entities;
using PropertyHub.Interfaces.IManager;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs; // Result<T>

namespace PropertyHub.Services
{
    public class FavoriteService:IFavoriteService
    {
        private readonly IFavoriteManager _manager;
        public FavoriteService(IFavoriteManager manager)
        {
            _manager=manager;
        }
        public Task<Result<string>> AddAsync(int userId, int propertyId, CancellationToken cancellationToken = default)
          =>_manager.AddAsync(userId,propertyId,cancellationToken);
        public Task<Result<string>> RemoveAsync(int userId, int propertyId, CancellationToken cancellationToken = default)
        =>_manager.RemoveAsync(userId,propertyId,cancellationToken);
        public Task<Result<List<FavoriteResponse>>> GetMyFavoritesAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
          =>_manager.GetMyFavoritesAsync(userId,pageNumber,pageSize,cancellationToken);
    }
}