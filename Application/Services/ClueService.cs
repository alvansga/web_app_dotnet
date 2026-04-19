using CodenameApp.Application.Interfaces;

public class ClueService
{
    private readonly IGameRoomRepository _roomRepo;

    public ClueService(IGameRoomRepository roomRepo)
    {
        _roomRepo = roomRepo;
    }

    public async Task AddClue(string code, string word, int count, Guid spymasterId)
    {
        var room = await _roomRepo.GetByCodeAsync(code);
        if (room == null) throw new Exception("Room not found");

        room.AddClue(word, count, spymasterId);
        await _roomRepo.SaveChangesAsync();
    }

    public async Task RemoveClue(string code, Guid clueId, Guid spymasterId)
    {
        var room = await _roomRepo.GetByCodeAsync(code);
        if (room == null) throw new Exception("Room not found");

        room.RemoveClue(clueId, spymasterId);
        await _roomRepo.SaveChangesAsync();
    }
}
