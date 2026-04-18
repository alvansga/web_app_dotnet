public class StartGameService
{
    private readonly IGameRoomRepository _repo;

    public StartGameService(IGameRoomRepository repo)
    {
        _repo = repo;
    }

    public async Task Execute(string code)
    {
        var room = await _repo.GetByCodeAsync(code);
        if (room == null) throw new Exception("Room not found");

        room.StartGame();

        await _repo.SaveChangesAsync();
    }
}