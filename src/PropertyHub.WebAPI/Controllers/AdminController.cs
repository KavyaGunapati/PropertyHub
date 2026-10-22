using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyHub.DataAccess.Enums;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs;

namespace PropertyHub.WebAPI.Controllers
{
    [ApiController]
    [Route("/api/admin")]
    [Authorize(Roles="Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _service;
        public AdminController(IAdminService service)
        {
            _service=service;
        }
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        {
            var result=await _service.GetDashboardAsync(cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(new {error=result.Error});
            }
            return Ok(new {data=result.Data});
        }
        [HttpGet("properties")]
        public async Task<IActionResult> GetProperties(PropertyStatus? status,int pageNumber,int pageSize,CancellationToken cancellationToken)
        {
            var result=await _service.GetPropertiesAsync(status,pageNumber,pageSize,cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(new {error=result.Error});
            }
            return Ok(new {data=result.Data});
        }
        [HttpPatch("properties/{id:int}/status")]
        public async Task<IActionResult> ChangePropertyStatus(int id,ChangePropertyStatusRequest request,CancellationToken cancellationToken)
        {
            var result=await _service.ChangePropertyStatusAsync(id,request.Status,cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(new {error=result.Error});
            }
            return Ok(new {data=result.Data});
        }
    }
}