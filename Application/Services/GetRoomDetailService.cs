using CodenameApp.Application.Interfaces;
using CodenameApp.Domain;

public class GetRoomDetailService
{
    private readonly IGameRoomRepository _repo;

    public GetRoomDetailService(IGameRoomRepository repo)
    {
        _repo = repo;
    }

    /// <param name="playerId">Optional: jika diisi dan player adalah Spymaster, semua card role akan ditampilkan</param>
    public async Task<RoomDetailResponse?> Execute(string code, Guid? playerId = null)
    {
        code = code.Trim().ToUpper();

        var room = await _repo.GetByCodeAsync(code);

        if (room == null)
            return null;

        bool isSpymaster = playerId.HasValue &&
            room.Players.Any(p => p.Id == playerId.Value && p.GameRole == PlayerGameRole.Spymaster);

        return MapToResponse(room, isSpymaster);
    }

    public async Task<List<RoomDetailResponse>> GetAllRooms()
    {
        var rooms = await _repo.GetAllAsync();
        return rooms.Select(room => MapToResponse(room, false)).ToList();
    }

    private static RoomDetailResponse MapToResponse(GameRoom room, bool isSpymaster)
    {
        return new RoomDetailResponse
        {
            Id = room.Id,
            Code = room.Code,
            Status = room.Status.ToString(),
            State = new GameStateDto
            {
                Phase = room.State.Phase.ToString(),
                Round = room.State.Round,
                IsStarted = room.State.IsStarted,
                Winner = room.State.Winner
            },
            Players = room.Players.Select(p => {
                var displayRole = p.GameRole.ToString();
                // Jika game sudah di phase Playing, siapapun yang 'None' otomatis dianggap FieldOperative
                if (room.State.Phase != GamePhase.SpymasterSelection && room.State.Phase != GamePhase.Empty && displayRole == "None") {
                    displayRole = "FieldOperative";
                }
                return new PlayerDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    GameRole = displayRole
                };
            }).ToList(),
            Cards = room.Cards.Select(c => new GameCardDto
            {
                Id = c.Id,
                Word = c.Word,
                // Spymaster melihat semua role. Field operative hanya melihat role yg sudah direveal.
                Role = (isSpymaster || c.IsRevealed) ? c.Role.ToString() : null,
                IsRevealed = c.IsRevealed
            }).ToList()
        };
    }
}