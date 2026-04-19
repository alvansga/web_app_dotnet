using CodenameApp.Application.Interfaces;

public class AssignRoleService
{
    private readonly IGameRoomRepository _roomRepo;

    public AssignRoleService(IGameRoomRepository roomRepo)
    {
        _roomRepo = roomRepo;
    }

    public async Task AssignSpymaster(string roomCode, Guid playerId)
    {
        var room = await _roomRepo.GetByCodeAsync(roomCode.ToUpper())
            ?? throw new Exception("Room not found");

        room.AssignSpymaster(playerId);
        await _roomRepo.SaveChangesAsync();
    }

    public async Task AssignFieldOperative(string roomCode, Guid playerId)
    {
        var room = await _roomRepo.GetByCodeAsync(roomCode.ToUpper())
            ?? throw new Exception("Room not found");

        room.AssignFieldOperative(playerId);
        await _roomRepo.SaveChangesAsync();
    }
}
