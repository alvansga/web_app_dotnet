using CodenameApp.Application.Interfaces;

public class RevealCardService
{
    private readonly IGameRoomRepository _roomRepo;

    public RevealCardService(IGameRoomRepository roomRepo)
    {
        _roomRepo = roomRepo;
    }

    public async Task Execute(string roomCode, Guid cardId, Guid playerId)
    {
        var room = await _roomRepo.GetByCodeAsync(roomCode.ToUpper())
            ?? throw new Exception("Room not found");

        room.RevealCard(cardId, playerId);
        await _roomRepo.SaveChangesAsync();
    }
}
