using CodenameApp.Application.Interfaces;

public class ClueService
{
    private readonly IGameRoomRepository _roomRepo;
    private readonly IPlayerRepository _playerRepo;

    public ClueService(IGameRoomRepository roomRepo, IPlayerRepository playerRepo)
    {
        _roomRepo = roomRepo;
        _playerRepo = playerRepo;
    }

    public async Task AddClue(string code, string word, int count, Guid spymasterId, string token)
    {
        await ValidatePlayer(spymasterId, token);

        var room = await _roomRepo.GetByCodeAsync(code);
        if (room == null) throw new Exception("Room not found");

        room.AddClue(word, count, spymasterId);
        await _roomRepo.SaveChangesAsync();
    }

    public async Task RemoveClue(string code, Guid clueId, Guid spymasterId, string token)
    {
        await ValidatePlayer(spymasterId, token);

        var room = await _roomRepo.GetByCodeAsync(code);
        if (room == null) throw new Exception("Room not found");

        room.RemoveClue(clueId, spymasterId);
        await _roomRepo.SaveChangesAsync();
    }

    private async Task ValidatePlayer(Guid playerId, string token)
    {
        var player = await _playerRepo.GetByIdAndTokenAsync(playerId, token)
            ?? throw new UnauthorizedAccessException("Invalid credentials");
    }
}
