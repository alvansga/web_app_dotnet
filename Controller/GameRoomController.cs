using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/rooms")]
public class GameRoomController : ControllerBase
{
    private readonly CreateRoomService _createService;
    private readonly JoinRoomService _joinService;
    private readonly GetRoomDetailService _getRoomDetailService;
    private readonly StartGameService _startGameService;

    public GameRoomController(
        CreateRoomService createService,
        JoinRoomService joinService,
        GetRoomDetailService getRoomDetailService,
        StartGameService startGameService)
    {
        _createService = createService;
        _joinService = joinService;
        _getRoomDetailService = getRoomDetailService;
        _startGameService = startGameService;
    }

    [HttpPost]
    public async Task<IActionResult> Create()
    {
        var code = await _createService.Execute();
        return Ok(new { code });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var rooms = await _getRoomDetailService.GetAllRooms();
        return Ok(rooms);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var result = await _getRoomDetailService.Execute(code);

        if (result == null)
            return NotFound("Room not found");

        return Ok(result);
    }

    [HttpPost("join")]
    public async Task<IActionResult> Join(JoinRoomRequest req)
    {
        var success = await _joinService.Execute(req.Code, req.PlayerId);

        if (!success)
            return BadRequest("Room or player not found");

        return Ok();
    }

    [HttpPost("{code}/start")]
    public async Task<IActionResult> Start(string code)
    {
        try
        {
            await _startGameService.Execute(code);
            return Ok(new { message = "Game started successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}