using CodenameApp.Application.Interfaces;

public class RevealCardService
{
    private readonly IGameRoomRepository _roomRepo;
    private readonly IPlayerRepository _playerRepo;

    public RevealCardService(IGameRoomRepository roomRepo, IPlayerRepository playerRepo)
    {
        _roomRepo = roomRepo;
        _playerRepo = playerRepo;
    }

    public async Task Execute(string roomCode, Guid cardId, Guid playerId, string token)
    {
        var player = await _playerRepo.GetByIdAndTokenAsync(playerId, token)
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        var room = await _roomRepo.GetByCodeAsync(roomCode.ToUpper())
            ?? throw new Exception("Room not found");

        room.RevealCard(cardId, playerId);
        await _roomRepo.SaveChangesAsync();
    }
}
