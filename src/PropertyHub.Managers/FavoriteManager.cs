using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyHub.DataAccess.Context;
using PropertyHub.DataAccess.Entities;
using PropertyHub.DataAccess.Enums;
using PropertyHub.Interfaces.IManager;
using PropertyHub.Models.DTOs;
namespace PropertyHub.Managers
{
    public class FavoriteManager:IFavoriteManager
    {
        private const int ManxPageSize=20;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<FavoriteManager> _logger;
        public FavoriteManager(AppDbContext context, IMapper mapper, ILogger<FavoriteManager> logger)
        {
            _context=context;
            _mapper=mapper;
            _logger=logger;
        }
        public async Task<Result<string>> AddAsync(int userId, int propertyId, CancellationToken cancellationToken = default)
        {
            var propertyExists=await _context.Properties.AnyAsync(p=>p.Id==propertyId && p.Status==PropertyStatus.Published,cancellationToken);
            if (!propertyExists)
            {
                _logger.LogWarning("User {UserId} tried to favorite unavailable property {PropertyId}", userId, propertyId);
                return Result<string>.Failure("Property Not found");
            }
            var alreadyFavorite=await _context.Favorites.AnyAsync(f=>f.UserId==userId && f.PropertyId==propertyId,cancellationToken);
           if (alreadyFavorite)
        {
            _logger.LogInformation("Property {PropertyId} already in favorites of user {UserId}", propertyId, userId);
            return Result<string>.Success("Already in favorites");
        }

        _context.Favorites.Add(new Favorite
        {
            UserId = userId,
            PropertyId = propertyId,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Concurrent duplicate favorite for user {UserId}, property {PropertyId}", userId, propertyId);
            _context.Entry(_context.Favorites.Local.Last()).State = EntityState.Detached;
            return Result<string>.Success("Already in favorites");
        }

        _logger.LogInformation("User {UserId} added property {PropertyId} to favorites", userId, propertyId);
        return Result<string>.Success("Added to favorites");
        }
        public async Task<Result<string>> RemoveAsync(int userId, int propertyId, CancellationToken cancellationToken = default)
        {
            var favorite=await _context.Favorites.Include(f=>f.Property).FirstOrDefaultAsync(f=>f.UserId==userId && f.PropertyId==propertyId, cancellationToken);
            if (favorite == null)
            {
                _logger.LogWarning("User {UserId} tried to remove non-existent favorite for property {PropertyId}", userId, propertyId);
                return Result<string>.Failure("Removed from favorites");
            }
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("User {UserId} removed property {PropertyId} from favorites", userId, propertyId);
            return Result<string>.Success("Successfuly removed from favorites");
        }
        public async Task<Result<List<FavoriteResponse>>> GetMyFavoritesAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
            {
                _logger.LogWarning("Favorites listing rejected for user {UserId}: invalid page number {PageNumber}", userId, pageNumber);
                return Result<List<FavoriteResponse>>.Failure("Page number should be 1 or greater than 1");
            }
            pageSize=Math.Clamp(pageSize,1,ManxPageSize);
            var favorites=await _context.Favorites.Where(f=>f.UserId==userId &&f.Property.Status==PropertyStatus.Published)
            .OrderByDescending(f=>f.CreatedAt)
            .ThenByDescending(f=>f.Id)
            .Skip((pageNumber-1)*pageSize)
            .Take(pageSize)
            .ProjectTo<FavoriteResponse>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
            _logger.LogInformation("Returned {Count} favorites for user {UserId}", favorites.Count, userId);
            return Result<List<FavoriteResponse>>.Success(favorites);

        }

    }
}