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
    private readonly GetAllPlayersService _getAllPlayersService;


    public PlayerController(CreatePlayerService service, GetAllPlayersService getAllPlayersService)
    {
        _service = service;
        _getAllPlayersService = getAllPlayersService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlayerRequest req)
    {
        var id = await _service.Execute(req.Name);
        return Ok(new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var players = await _getAllPlayersService.Execute();
        return Ok(players);
    }
}