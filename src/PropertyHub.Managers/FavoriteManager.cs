using Microsoft.EntityFrameworkCore;
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
        public FavoriteManager(AppDbContext context)
        {
            _context=context;
        }
        public async Task<Result<string>> AddAsync(int userId, int propertyId, CancellationToken cancellationToken = default)
        {
            var propertyExists=await _context.Properties.AnyAsync(p=>p.Id==propertyId && p.Status==PropertyStatus.Published,cancellationToken);
            if (!propertyExists)
            {
                return Result<string>.Failure("Property Not found");
            }
            var alreadyFavorite=await _context.Favorites.AnyAsync(f=>f.UserId==userId && f.PropertyId==propertyId,cancellationToken);
           if (alreadyFavorite)
            return Result<string>.Success("Already in favorites");

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
        catch (DbUpdateException)
        {
            _context.Entry(_context.Favorites.Local.Last()).State = EntityState.Detached;
            return Result<string>.Success("Already in favorites");
        }

        return Result<string>.Success("Added to favorites");
        }
        public async Task<Result<string>> RemoveAsync(int userId, int propertyId, CancellationToken cancellationToken = default)
        {
            var favorite=await _context.Favorites.Include(f=>f.Property).FirstOrDefaultAsync(f=>f.UserId==userId && f.PropertyId==propertyId, cancellationToken);
            if (favorite == null)
            {
                return Result<string>.Failure("Removed from favorites");
            }
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync(cancellationToken);
            return Result<string>.Success("Successfuly removed from favorites");
        }
        public async Task<Result<List<FavoriteResponse>>> GetMyFavoritesAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
            {
                return Result<List<FavoriteResponse>>.Failure("Page number should be 1 or greater than 1");
            }
            pageSize=Math.Clamp(pageSize,1,ManxPageSize);
            var favorites=await _context.Favorites.Where(f=>f.UserId==userId &&f.Property.Status==PropertyStatus.Published)
            .OrderByDescending(f=>f.CreatedAt)
            .ThenByDescending(f=>f.Id)
            .Skip((pageNumber-1)*pageSize)
            .Take(pageSize)
            .Select(f=>new FavoriteResponse
            {
               PropertyId = f.PropertyId,
                Title = f.Property.Title,
                City = f.Property.City,
                State = f.Property.State,
                Price = f.Property.Price,
                AddedAt = f.CreatedAt

            }).ToListAsync(cancellationToken);
            return Result<List<FavoriteResponse>>.Success(favorites);

        }

    }
}