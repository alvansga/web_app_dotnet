using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using CodenameApp.Application.Services;
using CodenameApp.DTOs;

namespace CodenameApp.Controllers;

[ApiController]
[Route("api/codenames")]
[EnableRateLimiting("fixed")]
public class CodenameController : ControllerBase
{
    private readonly CreateCodenameService _createService;
    private readonly GetCodenameService _getService;

    public CodenameController(
        CreateCodenameService createService,
        GetCodenameService getService)
    {
        _createService = createService;
        _getService = getService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCodenameRequest request)
    {
        var id = await _createService.Execute(request.Name, request.Description);
        return Ok(new { id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _getService.Execute(id);

        if (result == null)
            return NotFound();

        return Ok(new CodenameResponse
        {
            Id = result.Id,
            Name = result.Name,
            Description = result.Description
        });
    }
}