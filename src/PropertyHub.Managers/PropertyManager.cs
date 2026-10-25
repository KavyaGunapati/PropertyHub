using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyHub.DataAccess.Context;
using PropertyHub.DataAccess.Entities;
using PropertyHub.DataAccess.Enums;
using PropertyHub.Interfaces.IManager;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs;
using PropertyHub.Services.Validators;
using Microsoft.Extensions.Caching.Memory;
namespace PropertyHub.Managers
{
    public class PropertyManager:IPropertyManager
    {
        private readonly AppDbContext _context;
        private readonly PropertyValidator _propertyValidator;
        private readonly IFileUploadManager _fileManager;
        private const int MaxPageSize=30;
        private readonly IMapper _mapper;
        private readonly ILogger<PropertyManager> _logger;
        private readonly ICacheService _cacheService;
        public PropertyManager(AppDbContext context, PropertyValidator validator,IFileUploadManager fileManager,IMapper mapper,ILogger<PropertyManager> logger,ICacheService cacheService)
        {
            _context=context;
            _mapper=mapper;
            _logger=logger;
            _propertyValidator=validator;
            _fileManager=fileManager;
            _cacheService=cacheService;
        }
        public async Task<Result<PropertyResponse>> CreatePropertyAsync(CreatePropertyRequest request,int agentId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Agent {AgentId} creating property", agentId);
            var validationResult=await _propertyValidator.ValidateAsync(request,cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Property creation by agent {AgentId} failed validation", agentId);
                var erros=string.Join(" ",validationResult.Errors.Select(e=>e.ErrorMessage));
                return Result<PropertyResponse>.Failure(erros);
            }
            var agent=await _context.Users.FindAsync(new object[]{agentId}, cancellationToken);
            if(agent==null || agent.Role != UserRole.Agent)
            {
                _logger.LogWarning("Property creation rejected: user {AgentId} not found or not an agent", agentId);
                return Result<PropertyResponse>.Failure("Agent not found");
            }
            var property=_mapper.Map<Property>(request);
            property.AgentId=agentId;
            await _context.AddAsync(property,cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Property {PropertyId} created as draft by agent {AgentId}", property.Id, agentId);
            var response=_mapper.Map<PropertyResponse>(property);
            response.AgentName=agent.UserName;
            return Result<PropertyResponse>.Success(response);
        }
        public async Task<Result<PropertyResponse>> UpdatePropertyAsync(int propertyId, UpdatePropertyRequest request,int agentId,CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Agent {AgentId} updating property {PropertyId}", agentId, propertyId);
            var property=await _context.Properties.Include(p=>p.Agent).FirstOrDefaultAsync(p=>p.Id==propertyId,cancellationToken);
            if (property == null)
            {
                _logger.LogWarning("Update failed: property {PropertyId} not found", propertyId);
                return Result<PropertyResponse>.Failure("Property Not found");
            }
            if (property.Status == PropertyStatus.Deleted)
            {
                return Result<PropertyResponse>.Failure("Deleted property cannot be modified");
            }
            if (property.AgentId != agentId)
            {
                _logger.LogWarning("Agent {AgentId} not authorized to update property {PropertyId}", agentId, propertyId);
                return Result<PropertyResponse>.Failure("Unauthorized: You can only edit your own properties");
            }
            if (property.Status != PropertyStatus.Draft)
            {
                _logger.LogWarning("Update rejected: property {PropertyId} is {Status}, not draft", propertyId, property.Status);
                return Result<PropertyResponse>.Failure("Cannot edit published properties");
            }
            var validationResult=await _propertyValidator.ValidateAsync(_mapper.Map<CreatePropertyRequest>(request),cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Update of property {PropertyId} failed validation", propertyId);
                var errors=string.Join(", ",validationResult.Errors.Select(e=>e.ErrorMessage));
                return Result<PropertyResponse>.Failure(errors);
            }
            _mapper.Map(request, property);
            property.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _cacheService.Remove(CacheKeys.Property(propertyId));
            _logger.LogInformation("Property {PropertyId} updated by agent {AgentId}", propertyId, agentId);
            return Result<PropertyResponse>.Success(_mapper.Map<PropertyResponse>(property));
        }
        public async Task<Result<string>> DeletePropertyAsync(int propertyId, int agentId,CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Agent {AgentId} deleting property {PropertyId}", agentId, propertyId);
            var property=await _context.Properties.FindAsync(new object[]{propertyId},cancellationToken);
            if (property == null)
            {
                _logger.LogWarning("Delete failed: property {PropertyId} not found", propertyId);
                return Result<string>.Failure("Property Not found");
            }
            if (property.AgentId != agentId)
            {
                _logger.LogWarning("Agent {AgentId} not authorized to delete property {PropertyId}", agentId, propertyId);
                return Result<string>.Failure("Unauthorized: You can only edit your own properties");
            }
            if (property.Status != PropertyStatus.Draft)
            {
                _logger.LogWarning("Delete rejected: property {PropertyId} is {Status}, not draft", propertyId, property.Status);
                return Result<string>.Failure("Cannot delete published properties");
            }
            property.Status=PropertyStatus.Deleted;
            property.UpdatedAt=DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _cacheService.Remove(CacheKeys.Property(propertyId));
            _logger.LogInformation("Property {PropertyId} deleted by agent {AgentId}", propertyId, agentId);
            return Result<string>.Success("Property Deleted successfully");
        }
        public async Task<Result<PropertySearchResponse>> SearchPropertiesAsync(SearchPropertyRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Searching properties: page {PageNumber}, size {PageSize}, sort {SortBy} {SortOrder}", request.pageNumber, request.pageSize, request.SortBy, request.SortOrder);
            var cacheKey=CacheKeys.PropertySearch(request);
            var cachedSearch=_cacheService.Get<PropertySearchResponse>(cacheKey);
            if (cachedSearch != null)
            {
                _logger.LogInformation("Property search returned from cache");
                return Result<PropertySearchResponse>.Success(cachedSearch);
            }
            IQueryable<Property> query=_context.Properties
            .Include(p=>p.Agent)
            .Include(p=>p.Images.OrderBy(i=>i.DisplayOrder))
            .Where(i=>i.Status==PropertyStatus.Published);
            if (!string.IsNullOrWhiteSpace(request.City))
            query = query.Where(p => p.City.Contains(request.City));
        
        if (!string.IsNullOrWhiteSpace(request.State))
            query = query.Where(p => p.State == request.State);
            if (request.MinPrice.HasValue)
            {
                query=query.Where(p=>p.Price>=request.MinPrice);
            }
            if (request.MaxPrice.HasValue)
            {
                query=query.Where(p=>p.Price<=request.MaxPrice);
            }
            if (request.MinBedrooms.HasValue)
                query = query.Where(p => p.Bedrooms >= request.MinBedrooms);
        
            if (request.MaxBedrooms.HasValue)
                query = query.Where(p => p.Bedrooms <= request.MaxBedrooms);
        
            if (request.MinBathrooms.HasValue)
                query = query.Where(p => p.Bathrooms >= request.MinBathrooms);
        
            if (request.MaxBathrooms.HasValue)
                query = query.Where(p => p.Bathrooms <= request.MaxBathrooms);
            if (!string.IsNullOrWhiteSpace(request.searchTerm))
            {
                var term=request.searchTerm.ToLower();
                query=query.Where(p=>
                p.Title.ToLower().Contains(term)||
                p.Description.ToLower().Contains(term));
            }
            var totalCount=await query.CountAsync(cancellationToken);
            query=request.SortBy.ToLower() switch
            {
                "price"=>request.SortOrder=="asc"
                ?query.OrderBy(p=>p.Price):query.OrderByDescending(p=>p.Price),
                "squarefeet" => request.SortOrder == "asc"
                ? query.OrderBy(p => p.SquareFeet)
                : query.OrderByDescending(p => p.SquareFeet),
            
            _ => request.SortOrder == "asc"
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt)
            };
            var properties=await query.Skip((request.pageNumber-1)*request.pageSize)
            .Take(request.pageSize)
            .ToListAsync(cancellationToken);
            _logger.LogInformation("Property search returned {Count} of {TotalCount} properties", properties.Count, totalCount);
            var searchResponse=new PropertySearchResponse
            {
                Properties=properties.Select(p=>_mapper.Map<PropertyResponse>(p)).ToList(),
                TotalCount=totalCount,
                PageNumber=request.pageNumber,
                PageSize=request.pageSize
            };
            _cacheService.Set(cacheKey,searchResponse,TimeSpan.FromMinutes(5),CacheKeys.PropertySearchGroup);
            return Result<PropertySearchResponse>.Success(searchResponse);
        }
        public async Task<Result<PropertyResponse>> GetPropertyByIdAsync(int propertyId,CancellationToken cancellationToken = default)
        {
            var cacheKey=CacheKeys.Property(propertyId);
            var cachedProperty=_cacheService.Get<PropertyResponse>(cacheKey);
            if (cachedProperty != null)
            {
                _logger.LogInformation("Property {propertyId} returned from cache",propertyId);
                return Result<PropertyResponse>.Success(cachedProperty);
            }
            var property=await _context.Properties.Include(a=>a.Agent).Include(i=>i.Images.OrderBy(i=>i.DisplayOrder)).FirstOrDefaultAsync(p=>p.Id==propertyId,cancellationToken);
             if (property == null)
            {
                _logger.LogWarning("Property {PropertyId} not found", propertyId);
                return Result<PropertyResponse>.Failure("Property not found");
            }

        // 2. Check if published or agent's own property
        if (property.Status == PropertyStatus.Draft||property.Status==PropertyStatus.Deleted)
        {
            _logger.LogWarning("Property {PropertyId} requested but is not published", propertyId);
            return Result<PropertyResponse>.Failure("Property is not published");
        }
            _logger.LogInformation("Property {PropertyId} retrieved", propertyId);
            var response=_mapper.Map<PropertyResponse>(property);
            _cacheService.Set(cacheKey,response,TimeSpan.FromMinutes(10));
            return Result<PropertyResponse>.Success(response);
        }
        public async Task<Result<PropertyResponse>> PublishPropertyAsync(int propertyId, int agentId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Agent {AgentId} publishing property {PropertyId}", agentId, propertyId);
            var property=await _context.Properties.Include(p=>p.Agent).Include(p=>p.Images).FirstOrDefaultAsync(p=>p.Id==propertyId,cancellationToken);
            if (property == null)
            {
                _logger.LogWarning("Publish failed: property {PropertyId} not found", propertyId);
                return Result<PropertyResponse>.Failure("Property not found");
            }

            // 2. Verify ownership
            if (property.AgentId != agentId)
            {
                _logger.LogWarning("Agent {AgentId} not authorized to publish property {PropertyId}", agentId, propertyId);
                return Result<PropertyResponse>.Failure("Unauthorized: You can only publish your own properties");
            }
            if (property.Status == PropertyStatus.Deleted)
            {
                return Result<PropertyResponse>.Failure("Deleted property cannot be published");
            }
            // 3. Can only publish draft properties
            if (property.Status != PropertyStatus.Draft)
            {
                _logger.LogWarning("Publish rejected: property {PropertyId} is already {Status}", propertyId, property.Status);
                return Result<PropertyResponse>.Failure("Property is already published");
            }

            // 4. Require at least one image
            if (!property.Images.Any())
            {
                _logger.LogWarning("Publish rejected: property {PropertyId} has no images", propertyId);
                return Result<PropertyResponse>.Failure("Property must have at least one image before publishing");
            }
                 // 5. Publish
            property.Status = PropertyStatus.Published;
            property.UpdatedAt = DateTime.UtcNow;
        
            await _context.SaveChangesAsync(cancellationToken);
            _cacheService.Remove(CacheKeys.Property(propertyId));
            _cacheService.RemoveGroup(CacheKeys.PropertySearchGroup);
            _logger.LogInformation("Property {PropertyId} published by agent {AgentId}", propertyId, agentId);

            return Result<PropertyResponse>.Success(_mapper.Map<PropertyResponse>(property));

        }
        public async Task<Result<string>> EnsureCanAddImageAsync(int propertyId, int agentId, CancellationToken cancellationToken = default)
        {
            var property=await _context.Properties.FindAsync(new object[]{propertyId},cancellationToken);
            if (property == null)
            {
                _logger.LogWarning("Add-image check failed: property {PropertyId} not found", propertyId);
                return Result<string>.Failure("Property not found");
            }

            if (property.AgentId != agentId)
            {
                _logger.LogWarning("Agent {AgentId} not authorized to add images to property {PropertyId}", agentId, propertyId);
                return Result<string>.Failure("Unauthorized");
            }

            if (property.Status != PropertyStatus.Draft)
            {
                _logger.LogWarning("Add-image check rejected: property {PropertyId} is {Status}, not draft", propertyId, property.Status);
                return Result<string>.Failure("Cannot add images to published properties");
            }
                return Result<string>.Success("Ok");

        }
        public async Task<Result<PropertyImageResponse>> AddImageAsync(int propertyId, string imageUrl, CancellationToken cancellationToken = default)
        {
            var nextOrder=await _context.PropertyImages.Where(pi=>pi.PropertyId==propertyId).MaxAsync(pi=>(int?)pi.DisplayOrder,cancellationToken)??0;
            var image=new PropertyImage
            {
                PropertyId=propertyId,
                ImageUrl=imageUrl,
                DisplayOrder=nextOrder
            };
            await _context.PropertyImages.AddAsync(image,cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Image {ImageId} added to property {PropertyId}", image.Id, propertyId);
            return Result<PropertyImageResponse>.Success(new PropertyImageResponse
            {
                Id=image.Id,
                ImageUrl=image.ImageUrl,
                DisplayOrder=nextOrder,
            });
        }
        public async Task<Result<string>> DeleteImageAsync(int imageId, int agentId, CancellationToken cancellationToken = default)
        {
            var image=await _context.PropertyImages.Include(pi=>pi.Property)
            .FirstOrDefaultAsync(pi=>pi.Id==imageId,cancellationToken);
            if (image == null)
            {
                _logger.LogWarning("Image delete failed: image {ImageId} not found", imageId);
                return Result<string>.Failure("Image not found");
            }

            if (image.Property.AgentId != agentId)
            {
                _logger.LogWarning("Agent {AgentId} not authorized to delete image {ImageId}", agentId, imageId);
                return Result<string>.Failure("Unauthorized");
            }

            if (image.Property.Status != PropertyStatus.Draft)
            {
                _logger.LogWarning("Image delete rejected: property {PropertyId} is {Status}, not draft", image.PropertyId, image.Property.Status);
                return Result<string>.Failure("Cannot delete images from published properties");
            }
            var filename=Path.GetFileName(image.ImageUrl);
            _context.PropertyImages.Remove(image);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Image {ImageId} deleted by agent {AgentId}", imageId, agentId);
            return Result<string>.Success(filename);
        }
        public async Task<Result<List<PropertyResponse>>> GetMineAsync(int agentId, PropertyStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
            {
                _logger.LogWarning("Listing for agent {AgentId} rejected: invalid page number {PageNumber}", agentId, pageNumber);
                return Result<List<PropertyResponse>>.Failure("Page number should be greater than or equal to 1");
            }
            pageSize=Math.Clamp(pageSize,1,MaxPageSize);
            var query= _context.Properties.AsNoTracking().Where(p=>p.AgentId==agentId).Where(p=>p.Status!=PropertyStatus.Deleted);
            if (status.HasValue)
            {
                query=query.Where(p=>p.Status==status.Value);
            }
            var properties=await query
                           .Include(p=>p.Agent)
                           .Include(p=>p.Images.OrderBy(i=>i.DisplayOrder))
                           .OrderByDescending(p=>p.CreatedAt)
                           .ThenBy(p=>p.Id)
                           .Skip((pageNumber-1)*pageSize)
                           .Take(pageNumber)
                           .ToListAsync(cancellationToken);
            _logger.LogInformation("Returned {Count} properties for agent {AgentId}", properties.Count, agentId);
            return Result<List<PropertyResponse>>.Success(properties.Select(p=>_mapper.Map<PropertyResponse>(p)).ToList());

        }
    }
}