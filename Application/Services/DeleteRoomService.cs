using CodenameApp.Application.Interfaces;

public class DeleteRoomService
{
    private readonly IGameRoomRepository _repo;

    public DeleteRoomService(IGameRoomRepository repo)
    {
        _repo = repo;
    }

    public async Task Execute(string code)
    {
        var room = await _repo.GetByCodeAsync(code);
        if (room != null)
        {
            await _repo.DeleteAsync(room);
        }
    }

    public async Task CleanupEmptyRooms()
    {
        var rooms = await _repo.GetAllAsync();
        foreach (var room in rooms)
        {
            if (room.Players.Count == 0)
            {
                await _repo.DeleteAsync(room);
            }
        }
    }
}
