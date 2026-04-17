using Microsoft.AspNetCore.Mvc;
using CodenameApp.Application.Services;
using CodenameApp.DTOs;

using CodenameApp.Application.Interfaces;
using CodenameApp.Domain;

[ApiController]
[Route("api/players")]
public class PlayerController : ControllerBase
{
    private readonly CreatePlayerService _service;

    public PlayerController(CreatePlayerService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlayerRequest req)
    {
        var id = await _service.Execute(req.Name);
        return Ok(new { id });
    }
}