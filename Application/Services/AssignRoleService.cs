using CodenameApp.Application.Interfaces;
using CodenameApp.Domain;

public class AssignRoleService
{
    private readonly IGameRoomRepository _roomRepo;
    private readonly IPlayerRepository _playerRepo;

    public AssignRoleService(IGameRoomRepository roomRepo, IPlayerRepository playerRepo)
    {
        _roomRepo = roomRepo;
        _playerRepo = playerRepo;
    }

    public async Task AssignRole(string roomCode, Guid playerId, PlayerGameRole role, string token)
    {
        await ValidatePlayer(playerId, token);

        var room = await _roomRepo.GetByCodeAsync(roomCode.ToUpper())
            ?? throw new Exception("Room not found");

        room.AssignRole(playerId, role);
        await _roomRepo.SaveChangesAsync();
    }

    public async Task AssignSpymaster(string roomCode, Guid playerId, string token)
    {
        // Backward compat: default to RedSpymaster if no team specified
        await AssignRole(roomCode, playerId, PlayerGameRole.RedSpymaster, token);
    }

    public async Task AssignFieldOperative(string roomCode, Guid playerId, string token)
    {
        // Backward compat: default to RedFieldOperative if no team specified
        await AssignRole(roomCode, playerId, PlayerGameRole.RedFieldOperative, token);
    }

    public async Task AssignRedSpymaster(string roomCode, Guid playerId, string token)
    {
        await AssignRole(roomCode, playerId, PlayerGameRole.RedSpymaster, token);
    }

    public async Task AssignBlueSpymaster(string roomCode, Guid playerId, string token)
    {
        await AssignRole(roomCode, playerId, PlayerGameRole.BlueSpymaster, token);
    }

    public async Task AssignRedFieldOperative(string roomCode, Guid playerId, string token)
    {
        await AssignRole(roomCode, playerId, PlayerGameRole.RedFieldOperative, token);
    }

    public async Task AssignBlueFieldOperative(string roomCode, Guid playerId, string token)
    {
        await AssignRole(roomCode, playerId, PlayerGameRole.BlueFieldOperative, token);
    }

    private async Task ValidatePlayer(Guid playerId, string token)
    {
        var player = await _playerRepo.GetByIdAndTokenAsync(playerId, token)
            ?? throw new UnauthorizedAccessException("Invalid credentials");
    }
}