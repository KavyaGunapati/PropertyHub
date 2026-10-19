using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyHub.Interfaces.IService;

namespace PropertyHub.WebAPI.Controllers
{
    [ApiController]
    [Route("api/favorities")]
    [Authorize]
    public class FavoriteController : ControllerBase
    {
        private readonly IFavoriteService _service;
        public FavoriteController(IFavoriteService service){
            _service=service;
        }
        private bool TryGetUserId(out int userId)
          =>int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out userId);
        [HttpPost("{propertyId:int}")]
        public async Task<IActionResult> AddToFavorite(int propertyId, CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _service.AddAsync(userId, propertyId, cancellationToken);
            if (!result.IsSuccess)
               return BadRequest(new { error = result.Error });

            return Ok(new { message = result.Data });
        }
        [HttpDelete("{propertyId:int}")]
        public async Task<IActionResult> RemoveFromFavorite(int propertyId, CancellationToken cancellationToken)
        {
            if(!TryGetUserId(out var userId))
                return Unauthorized();
            var result=await _service.RemoveAsync(userId,propertyId,cancellationToken);
            if(!result.IsSuccess)
                return BadRequest(new {error=result.Error});
            return Ok(new {message=result.Data});
        }
        [HttpGet]
        public async Task<IActionResult> GetMyFavorities(int pageNumber=1,int pageSize=10,CancellationToken cancellationToken=default)
        {
            if(!TryGetUserId(out var userId))
                return Unauthorized();
            var result=await _service.GetMyFavoritesAsync(userId,pageNumber,pageSize,cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(new {error=result.Error});
            }
            return Ok(new {message=result.Data});
        }

    }
}