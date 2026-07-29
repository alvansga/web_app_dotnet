using CodenameApp.Application.Interfaces;

public class AssignRoleService
{
    private readonly IGameRoomRepository _roomRepo;
    private readonly IPlayerRepository _playerRepo;

    public AssignRoleService(IGameRoomRepository roomRepo, IPlayerRepository playerRepo)
    {
        _roomRepo = roomRepo;
        _playerRepo = playerRepo;
    }

    public async Task AssignSpymaster(string roomCode, Guid playerId, string token)
    {
        await ValidatePlayer(playerId, token);

        var room = await _roomRepo.GetByCodeAsync(roomCode.ToUpper())
            ?? throw new Exception("Room not found");

        room.AssignSpymaster(playerId);
        await _roomRepo.SaveChangesAsync();
    }

    public async Task AssignFieldOperative(string roomCode, Guid playerId, string token)
    {
        await ValidatePlayer(playerId, token);

        var room = await _roomRepo.GetByCodeAsync(roomCode.ToUpper())
            ?? throw new Exception("Room not found");

        room.AssignFieldOperative(playerId);
        await _roomRepo.SaveChangesAsync();
    }

    private async Task ValidatePlayer(Guid playerId, string token)
    {
        var player = await _playerRepo.GetByIdAndTokenAsync(playerId, token)
            ?? throw new UnauthorizedAccessException("Invalid credentials");
    }
}
