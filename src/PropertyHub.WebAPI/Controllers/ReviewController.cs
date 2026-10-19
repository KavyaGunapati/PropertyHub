using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs;

namespace PropertyHub.WebAPI.Controllers
{
    [ApiController]
    [Route("/api/properties/{propertyId:int}/reviews")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _service;
        public ReviewController(IReviewService service)
        {
            _service=service;
        }
        private bool TryGetUserId(out int userId)
           =>int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out userId);
        [HttpPost]
        public async Task<IActionResult> SubmitReview(int propertyId,CreateReviewRequest request,CancellationToken cancellationToken)
        {
            if(!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }
            var result=await _service.SubmitReviewAsync(propertyId,userId,request,cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(new {error=result.Error});
            }
            return Ok(new {data=result.Data});
        }
        [HttpGet]
        public async Task<IActionResult> GetReviews(int propertyId,int pageNumber=1,int pageSize=10,CancellationToken cancellationToken=default)
        {
            var result=await _service.GetReviewsAsync(propertyId,pageNumber,pageSize,cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(new {error=result.Error});
            }
            return Ok(new {data=result.Data});
        }
    }
}