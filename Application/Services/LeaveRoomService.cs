using CodenameApp.Application.Interfaces;

public class LeaveRoomService
{
    private readonly IPlayerRepository _playerRepo;

    public LeaveRoomService(IPlayerRepository playerRepo)
    {
        _playerRepo = playerRepo;
    }

    public async Task Execute(Guid playerId, string token)
    {
        var player = await _playerRepo.GetByIdAndTokenAsync(playerId, token)
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        player.LeaveRoom();
        await _playerRepo.SaveChangesAsync();
    }
}
