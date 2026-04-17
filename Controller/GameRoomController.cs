using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/rooms")]
public class GameRoomController : ControllerBase
{
    private readonly CreateRoomService _createService;
    private readonly JoinRoomService _joinService;

    public GameRoomController(
        CreateRoomService createService,
        JoinRoomService joinService)
    {
        _createService = createService;
        _joinService = joinService;
    }

    [HttpPost]
    public async Task<IActionResult> Create()
    {
        var code = await _createService.Execute();
        return Ok(new { code });
    }

    [HttpPost("join")]
    public async Task<IActionResult> Join(JoinRoomRequest req)
    {
        var success = await _joinService.Execute(req.Code, req.PlayerId);

        if (!success)
            return BadRequest("Room or player not found");

        return Ok();
    }
}