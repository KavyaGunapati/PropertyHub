using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PropertyHub.DataAccess.Context;
using PropertyHub.DataAccess.Entities;
using PropertyHub.DataAccess.Enums;
using PropertyHub.Interfaces.IManager;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs;
using PropertyHub.Services.Validators;
namespace PropertyHub.Managers
{
    public class PropertyManager:IPropertyManager
    {
        private readonly AppDbContext _context;
        private readonly PropertyValidator _propertyValidator;
        private readonly IFileUploadManager _fileManager;
        private const int MaxPageSize=30;
        public PropertyManager(AppDbContext context, PropertyValidator validator,IFileUploadManager fileManager)
        {
            _context=context;
            _propertyValidator=validator;
            _fileManager=fileManager;
        }
        public async Task<Result<PropertyResponse>> CreatePropertyAsync(CreatePropertyRequest request,int agentId, CancellationToken cancellationToken = default)
        {
            var validationResult=await _propertyValidator.ValidateAsync(request,cancellationToken);
            if (!validationResult.IsValid)
            {
                var erros=string.Join(" ",validationResult.Errors.Select(e=>e.ErrorMessage));
                return Result<PropertyResponse>.Failure(erros);
            }
            var agent=await _context.Users.FindAsync(new object[]{agentId}, cancellationToken);
            if(agent==null || agent.Role != UserRole.Agent)
            {
                return Result<PropertyResponse>.Failure("Agent not found");
            }
            var property=new Property
            {
                AgentId=agentId,
                Title=request.Title,
                Description=request.Description,
                Address=request.Address,
                City = request.City,
                State = request.State,
                ZipCode = request.ZipCode,
                Price = request.Price,
                Bedrooms = request.Bedrooms,
                Bathrooms = request.Bathrooms,
                SquareFeet = request.SquareFeet,
                YearBuilt = request.YearBuilt,
                Status = PropertyStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };
            await _context.AddAsync(property,cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return Result<PropertyResponse>.Success(MapToResponse(property,agent.UserName));
        }
        public async Task<Result<PropertyResponse>> UpdatePropertyAsync(int propertyId, UpdatePropertyRequest request,int agentId,CancellationToken cancellationToken = default)
        {
            var property=await _context.Properties.Include(p=>p.Agent).FirstOrDefaultAsync(p=>p.Id==propertyId,cancellationToken);
            if (property == null)
            {
                return Result<PropertyResponse>.Failure("Property Not found");
            }
            if (property.AgentId != agentId)
            {
                return Result<PropertyResponse>.Failure("Unauthorized: You can only edit your own properties");
            }
            if (property.Status != PropertyStatus.Draft)
            {
                return Result<PropertyResponse>.Failure("Cannot edit published properties");
            }
            var validationResult=await _propertyValidator.ValidateAsync(
                new CreatePropertyRequest
                {
                    Title = request.Title,
                Description = request.Description,
                Address = request.Address,
                City = request.City,
                State = request.State,
                ZipCode = request.ZipCode,
                Price = request.Price,
                Bedrooms = request.Bedrooms,
                Bathrooms = request.Bathrooms,
                SquareFeet = request.SquareFeet,
                YearBuilt = request.YearBuilt
                },cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors=string.Join(", ",validationResult.Errors.Select(e=>e.ErrorMessage));
                return Result<PropertyResponse>.Failure(errors);
            }
            property.Title=request.Title;
            property.Description = request.Description;
            property.Address = request.Address;
            property.City = request.City;
            property.State = request.State;
            property.ZipCode = request.ZipCode;
            property.Price = request.Price;
            property.Bedrooms = request.Bedrooms;
            property.Bathrooms = request.Bathrooms;
            property.SquareFeet = request.SquareFeet;
            property.YearBuilt = request.YearBuilt;
            property.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return Result<PropertyResponse>.Success(MapToResponse(property, property.Agent.UserName));
        }
        public async Task<Result<string>> DeletePropertyAsync(int propertyId, int agentId,CancellationToken cancellationToken = default)
        {
            var property=await _context.Properties.FindAsync(new object[]{propertyId},cancellationToken);
            if (property == null)
            {
                return Result<string>.Failure("Property Not found");
            }
            if (property.AgentId != agentId)
            {
                return Result<string>.Failure("Unauthorized: You can only edit your own properties");
            }
            if (property.Status != PropertyStatus.Draft)
            {
                return Result<string>.Failure("Cannot delete published properties");
            }
            _context.Properties.Remove(property);
            await _context.SaveChangesAsync(cancellationToken);
            return Result<string>.Success("Property Deleted successfully");
        }
        public async Task<Result<PropertySearchResponse>> SearchPropertiesAsync(SearchPropertyRequest request, CancellationToken cancellationToken = default)
        {
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
            return Result<PropertySearchResponse>.Success(new PropertySearchResponse
            {
                Properties=properties.Select(p=>MapToResponse(p,p.Agent.UserName)).ToList(),
                TotalCount=totalCount,
                PageNumber=request.pageNumber,
                PageSize=request.pageSize
            });
        }
        public async Task<Result<PropertyResponse>> GetPropertyByIdAsync(int propertyId,CancellationToken cancellationToken = default)
        {
            var property=await _context.Properties.Include(a=>a.Agent).Include(i=>i.Images.OrderBy(i=>i.DisplayOrder)).FirstOrDefaultAsync(p=>p.Id==propertyId,cancellationToken);
             if (property == null)
            return Result<PropertyResponse>.Failure("Property not found");
        
        // 2. Check if published or agent's own property
        if (property.Status == PropertyStatus.Draft)
            return Result<PropertyResponse>.Failure("Property is not published");
            return Result<PropertyResponse>.Success(MapToResponse(property,property.Agent.UserName));
        }
        public async Task<Result<PropertyResponse>> PublishPropertyAsync(int propertyId, int agentId, CancellationToken cancellationToken = default)
        {
            var property=await _context.Properties.Include(p=>p.Agent).Include(p=>p.Images).FirstOrDefaultAsync(p=>p.Id==propertyId,cancellationToken);
            if (property == null)
            return Result<PropertyResponse>.Failure("Property not found");
        
            // 2. Verify ownership
            if (property.AgentId != agentId)
                return Result<PropertyResponse>.Failure("Unauthorized: You can only publish your own properties");
        
            // 3. Can only publish draft properties
            if (property.Status != PropertyStatus.Draft)
                return Result<PropertyResponse>.Failure("Property is already published");
        
            // 4. Require at least one image
            if (!property.Images.Any())
                return Result<PropertyResponse>.Failure("Property must have at least one image before publishing");
                 // 5. Publish
            property.Status = PropertyStatus.Published;
            property.UpdatedAt = DateTime.UtcNow;
        
            await _context.SaveChangesAsync(cancellationToken);
        
            return Result<PropertyResponse>.Success(MapToResponse(property, property.Agent.UserName));

        }
        public async Task<Result<string>> EnsureCanAddImageAsync(int propertyId, int agentId, CancellationToken cancellationToken = default)
        {
            var property=await _context.Properties.FindAsync(new object[]{propertyId},cancellationToken);
            if (property == null)
                return Result<string>.Failure("Property not found");

            if (property.AgentId != agentId)
                return Result<string>.Failure("Unauthorized");

            if (property.Status != PropertyStatus.Draft)
                return Result<string>.Failure("Cannot add images to published properties");
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
                return Result<string>.Failure("Image not found");

            if (image.Property.AgentId != agentId)
                return Result<string>.Failure("Unauthorized");

            if (image.Property.Status != PropertyStatus.Draft)
                return Result<string>.Failure("Cannot delete images from published properties");
            var filename=Path.GetFileName(image.ImageUrl);
            _context.PropertyImages.Remove(image);
            await _context.SaveChangesAsync(cancellationToken);
            return Result<string>.Success(filename);
        }
        public async Task<Result<List<PropertyResponse>>> GetMineAsync(int agentId, PropertyStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
            {
                return Result<List<PropertyResponse>>.Failure("Page number should be greater than or equal to 1");
            }
            pageSize=Math.Clamp(pageSize,1,MaxPageSize);
            var query= _context.Properties.AsNoTracking().Where(p=>p.Id==agentId);
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
            return Result<List<PropertyResponse>>.Success(properties.Select(p=>MapToResponse(p,p.Agent.UserName)).ToList());

        }
        private PropertyResponse MapToResponse(Property property, string agentName)
        {
            return new PropertyResponse
            {
               Id = property.Id,
            AgentId = property.AgentId,
            AgentName = agentName,
            Title = property.Title,
            Description = property.Description,
            Address = property.Address,
            City = property.City,
            State = property.State,
            ZipCode = property.ZipCode,
            Price = property.Price,
            Bedrooms = property.Bedrooms,
            Bathrooms = property.Bathrooms,
            SquareFeet = property.SquareFeet,
            YearBuilt = property.YearBuilt,
            Status = property.Status,
            CreatedAt = property.CreatedAt,
            UpdatedAt = property.UpdatedAt,
            Images=property.Images
            .OrderBy(o=>o.DisplayOrder)
            .Select(i=>new PropertyImageResponse
            {
                Id=i.Id,
                ImageUrl=i.ImageUrl,
                DisplayOrder=i.DisplayOrder
            }).ToList()
            };
        }
    }
}