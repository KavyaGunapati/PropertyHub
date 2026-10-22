using System.Security.Claims;
using PropertyHub.Services.Validators;
using Microsoft.AspNetCore.Mvc;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using PropertyHub.DataAccess.Enums;

namespace PropertyHub.WebAPI.Controllers
{
    [ApiController]
    [Route("api/property")]
    public class PropertyController:ControllerBase
    {
        private readonly IPropertyService _service;
        private readonly PropertyValidator _validator;
        public PropertyController(IPropertyService service,PropertyValidator validator)
        {
            _service=service;
            _validator=validator;
        }
        private bool TryGetUserId(out int userId)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }
        [Authorize]
        [HttpPost("create-property")]
        public async Task<IActionResult> CreateProperty(CreatePropertyRequest request,int agentId,CancellationToken cancellationToken)
        {
            var result=await _service.CreatePropertyAsync(request,agentId,cancellationToken);
            if(!result.IsSuccess)
                 return BadRequest(new {error=result.Error});
            return CreatedAtAction(nameof(GetPropertyById),new{id=result.Data.Id},new {message="property created successfully",data=result.Data} );
        }
        [HttpGet("get-property/{id}")]
        public async Task<IActionResult> GetPropertyById(int id,CancellationToken cancellationToken)
        {
            var result = await _service.GetPropertyByIdAsync(id, cancellationToken);
        
            if (!result.IsSuccess)
                return NotFound(new { error = result.Error });
        
            return Ok(new { data = result.Data });
        }
        [HttpGet("search")]
        public async Task<IActionResult> SearchProperties(SearchPropertyRequest request,CancellationToken cancellationToken)
        {
            var properties=await _service.SearchPropertiesAsync(request,cancellationToken);
            if(!properties.IsSuccess)
                return BadRequest(new {error=properties.Error});
            return Ok(new {data=properties.Data});
        }
        [Authorize]
        [HttpPut("{id}")]
         public async Task<IActionResult> UpdateProperty(int id,[FromBody] UpdatePropertyRequest request,CancellationToken cancellationToken)
        {
            if(!TryGetUserId(out var agentId))
                return Unauthorized();
            var result=await _service.UpdatePropertyAsync(id,request,agentId,cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(new {error=result.Error});
            }
            return Ok(new {message="updated successfully",data=result.Data});
        }
        [Authorize]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteProperty(int id, CancellationToken cancellationToken)
        {
            if(!TryGetUserId(out var agentId))
               return Unauthorized();
            var result=await _service.DeletePropertyAsync(id,agentId,cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(new {error=result.Error});
            }
            return Ok(new {data=result.Data});
        }
        [Authorize]
        [HttpPost("{id}/publish")]
        public async Task<IActionResult> PublishProperty(int id, CancellationToken cancellationToken)
        {
            if(!TryGetUserId(out var agentId))
                return Unauthorized();
            var result=await _service.PublishPropertyAsync(id,agentId,cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(new {error=result.Error});
            }
            return Ok(new { message = "Property published", data = result.Data });
        }
        [Authorize]
        [HttpPost("{id:int}/images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(int id,IFormFile file,CancellationToken cancellationToken)
        {
            if(!TryGetUserId(out var agentId))
                return Unauthorized();
            var result=await _service.UploadPropertyImageAsync(id, file,agentId,cancellationToken);
            if(!result.IsSuccess)
               return BadRequest(new {error=result.Error});
            return Ok(new { message = "Image uploaded", data = result.Data });
        }
        [Authorize]
        [HttpDelete("images/{imageId:int}")]
        public async Task<IActionResult> DeleteImage(int imageId, CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var agentId))
                return Unauthorized();

            var result = await _service.DeletePropertyImageAsync(imageId, agentId, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(new {error=result.Error});
            return Ok(new { message = result.Data });
        }
        [Authorize]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine(PropertyStatus? status, int pageNumber = 1,int pageSize = 10,CancellationToken cancellationToken = default)
        {
            if (!TryGetUserId(out var agentId))
                return Unauthorized();

            var result = await _service.GetMineAsync(agentId, status, pageNumber, pageSize, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(new {error=result.Error});

            return Ok(new { data = result.Data });
        }
    }
}