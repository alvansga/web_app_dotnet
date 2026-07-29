using CodenameApp.Application.Interfaces;
using CodenameApp.Domain;
using CodenameApp.Infrastructure.Data;

public class JoinRoomService
{
    private readonly IGameRoomRepository _roomRepo;
    private readonly IPlayerRepository _playerRepo;

    public JoinRoomService(
        IGameRoomRepository roomRepo,
        IPlayerRepository playerRepo)
    {
        _roomRepo = roomRepo;
        _playerRepo = playerRepo;
    }

    public async Task<bool> Execute(string code, Guid playerId, string token)
    {
        var room = await _roomRepo.GetByCodeAsync(code);
        if (room == null) return false;

        var player = await _playerRepo.GetByIdAndTokenAsync(playerId, token);
        if (player == null) return false;

        room.AddPlayer(player);

        await _roomRepo.SaveChangesAsync();

        return true;
    }
}