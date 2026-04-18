using CodenameApp.Application.Interfaces;

public class GetRoomDetailService
{
    private readonly IGameRoomRepository _repo;

    public GetRoomDetailService(IGameRoomRepository repo)
    {
        _repo = repo;
    }

    public async Task<RoomDetailResponse?> Execute(string code)
    {
        code = code.Trim().ToUpper();

        var room = await _repo.GetByCodeAsync(code);

        if (room == null)
            return null;

        return new RoomDetailResponse
        {
            Id = room.Id,
            Code = room.Code,
            Status = room.Status,
            State = new GameStateDto
            {
                Phase = room.State.Phase,
                Round = room.State.Round,
                IsStarted = room.State.IsStarted
            },
            Players = room.Players.Select(p => new PlayerDto
            {
                Id = p.Id,
                Name = p.Name
            }).ToList()
        };
    }
}