using CodenameApp.Application.Interfaces;

public class LeaveRoomService
{
    private readonly IPlayerRepository _playerRepo;

    public LeaveRoomService(IPlayerRepository playerRepo)
    {
        _playerRepo = playerRepo;
    }

    public async Task Execute(Guid playerId)
    {
        var player = await _playerRepo.GetByIdAsync(playerId);
        if (player != null)
        {
            player.LeaveRoom();
            await _playerRepo.SaveChangesAsync();
        }
    }
}
