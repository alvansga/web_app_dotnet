using CodenameApp.Application.Interfaces;
using CodenameApp.Domain;

public class GetOnlineStatsService
{
    private readonly IGameRoomRepository _repo;

    public GetOnlineStatsService(IGameRoomRepository repo)
    {
        _repo = repo;
    }

    public async Task<OnlineStatsResponse> Execute()
    {
        var rooms = await _repo.GetAllAsync();

        int inLobby = 0;
        int playing = 0;

        foreach (var room in rooms)
        {
            int playerCount = room.Players.Count;
            if (room.Status == RoomStatus.Waiting)
                inLobby += playerCount;
            else
                playing += playerCount;
        }

        return new OnlineStatsResponse
        {
            TotalOnline = inLobby + playing,
            InLobby = inLobby,
            Playing = playing,
            TotalRooms = rooms.Count
        };
    }
}