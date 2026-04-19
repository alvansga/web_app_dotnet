using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/rooms")]
public class GameRoomController : ControllerBase
{
    private readonly CreateRoomService _createService;
    private readonly JoinRoomService _joinService;
    private readonly GetRoomDetailService _getRoomDetailService;
    private readonly StartGameService _startGameService;
    private readonly AssignRoleService _assignRoleService;
    private readonly RevealCardService _revealCardService;

    public GameRoomController(
        CreateRoomService createService,
        JoinRoomService joinService,
        GetRoomDetailService getRoomDetailService,
        StartGameService startGameService,
        AssignRoleService assignRoleService,
        RevealCardService revealCardService)
    {
        _createService = createService;
        _joinService = joinService;
        _getRoomDetailService = getRoomDetailService;
        _startGameService = startGameService;
        _assignRoleService = assignRoleService;
        _revealCardService = revealCardService;
    }

    [HttpPost]
    public async Task<IActionResult> Create()
    {
        var code = await _createService.Execute();
        return Ok(new { code });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromServices] DeleteRoomService deleteService)
    {
        // Cleanup empty rooms before returning list
        await deleteService.CleanupEmptyRooms();
        
        var rooms = await _getRoomDetailService.GetAllRooms();
        return Ok(rooms);
    }

    [HttpDelete("{code}")]
    public async Task<IActionResult> Delete(string code, [FromServices] DeleteRoomService deleteService)
    {
        await deleteService.Execute(code);
        return Ok(new { message = "Room deleted" });
    }

    /// <param name="playerId">Optional: pass Player ID untuk mendapat room detail sesuai peran (spymaster/field-operative)</param>
    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code, [FromQuery] Guid? playerId = null)
    {
        var result = await _getRoomDetailService.Execute(code, playerId);

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

    [HttpPost("leave")]
    public async Task<IActionResult> Leave([FromBody] LeaveRoomRequest req, [FromServices] LeaveRoomService leaveService)
    {
        await leaveService.Execute(req.PlayerId);
        return Ok(new { message = "You left the room" });
    }

    [HttpPost("{code}/start")]
    public async Task<IActionResult> Start(string code, [FromBody] StartGameRequest req)
    {
        try
        {
            await _startGameService.Execute(code, req.PlayerId);
            return Ok(new { message = "Game started! Roles assigned automatically." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Assign player sebagai Spymaster di room</summary>
    [HttpPost("{code}/assign-spymaster")]
    public async Task<IActionResult> AssignSpymaster(string code, [FromBody] AssignRoleRequest req)
    {
        try
        {
            await _assignRoleService.AssignSpymaster(code, req.PlayerId);
            return Ok(new { message = "You are now the Spymaster!" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Assign player sebagai Field Operative di room</summary>
    [HttpPost("{code}/assign-field-operative")]
    public async Task<IActionResult> AssignFieldOperative(string code, [FromBody] AssignRoleRequest req)
    {
        try
        {
            await _assignRoleService.AssignFieldOperative(code, req.PlayerId);
            return Ok(new { message = "You are now a Field Operative!" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Field Operative reveal sebuah kartu</summary>
    [HttpPost("{code}/reveal/{cardId}")]
    public async Task<IActionResult> RevealCard(string code, Guid cardId, [FromBody] RevealCardRequest req)
    {
        try
        {
            await _revealCardService.Execute(code, cardId, req.PlayerId);
            return Ok(new { message = "Card revealed!" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{code}/clue")]
    public async Task<IActionResult> AddClue(string code, [FromBody] AddClueRequest req, [FromServices] ClueService clueService)
    {
        try
        {
            await clueService.AddClue(code, req.Word, req.Count, req.PlayerId);
            return Ok(new { message = "Clue added!" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{code}/clue/{clueId}")]
    public async Task<IActionResult> RemoveClue(string code, Guid clueId, [FromQuery] Guid playerId, [FromServices] ClueService clueService)
    {
        try
        {
            await clueService.RemoveClue(code, clueId, playerId);
            return Ok(new { message = "Clue removed!" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}