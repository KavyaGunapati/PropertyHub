using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyHub.Models.DTOs;
using PropertyHub.Services;

namespace PropertyHub.WebAPI.Controllers
{
    [ApiController]
    [Route("/api/inquiries")]
    [Authorize]
    public class InquiryController : ControllerBase
    {
        private readonly InquiryService _service;
        public InquiryController(InquiryService service)
        {
            _service=service;
        }
        private bool TryGetUserId(out int userId)
          =>int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out userId);
        [HttpPost("{propertyId:int}")]
        public async Task<IActionResult> Submit(int propertyId,CreateInquiryRequest request,CancellationToken cancellationToken)
        {
            if(!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }
            var result = await _service.SubmitAsync(propertyId, userId, request, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(new {error=result.Error});

            return StatusCode(201, new { message = "Inquiry submitted", data = result.Data });
        }
        [HttpGet("{propertyId:int}")]
        public async Task<IActionResult>  GetForProperty(int propertyId,int pageNumber=1,int pageSize=10,CancellationToken cancellationToken=default)
        {
            if(!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }
            var result=await _service.GetForPropertyAsync(propertyId,userId,pageNumber,pageSize,cancellationToken);
            if (!result.IsSuccess)
            {
                BadRequest(new {error=result.Error});
            }
            return Ok(new {data=result.Data});
        }
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine(int pageNumber=1,int pageSize=10,CancellationToken cancellationToken=default)
        {
            if(!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }
            var result=await _service.GetMineAsync(userId,pageNumber,pageSize,cancellationToken);
            if (!result.IsSuccess)
            {
                return Unauthorized();
            }
            return Ok(new {data=result.Data});
        }
        [HttpPost("{id:int}/respond")]
        public async Task<IActionResult> Respond(int id,RespondInquiryRequest request,CancellationToken cancellationToken)
        {
            if(!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }
            var result=await _service.RespondAsync(id,userId,request,cancellationToken);
            if (!result.IsSuccess)
            {
                return Unauthorized();
            }
            return Ok(new {data=result.Data});
        }
    }
}