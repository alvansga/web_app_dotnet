using CodenameApp.Application.Interfaces;
using CodenameApp.Domain;

public class GetRoomDetailService
{
    private readonly IGameRoomRepository _repo;
    private readonly IPlayerRepository _playerRepo;

    public GetRoomDetailService(IGameRoomRepository repo, IPlayerRepository playerRepo)
    {
        _repo = repo;
        _playerRepo = playerRepo;
    }

    /// <param name="playerId">Optional: jika diisi dan player adalah Spymaster, semua card role akan ditampilkan</param>
    /// <param name="token">Required jika playerId diisi — untuk validasi identitas</param>
    public async Task<RoomDetailResponse?> Execute(string code, Guid? playerId = null, string? token = null)
    {
        code = code.Trim().ToUpper();

        var room = await _repo.GetByCodeAsync(code);

        if (room == null)
            return null;

        bool isSpymaster = false;
        if (playerId.HasValue && !string.IsNullOrEmpty(token))
        {
            var player = await _playerRepo.GetByIdAndTokenAsync(playerId.Value, token);
            if (player != null)
            {
                var me = room.Players.FirstOrDefault(p => p.Id == playerId.Value);
                isSpymaster = me != null && (
                    me.GameRole == PlayerGameRole.RedSpymaster ||
                    me.GameRole == PlayerGameRole.BlueSpymaster ||
                    me.GameRole == PlayerGameRole.Spymaster);
            }
        }

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
                Winner = room.State.Winner,
                CurrentTurn = room.State.CurrentTurn
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
                Role = (isSpymaster || c.IsRevealed || room.State.Phase == GamePhase.Ended) ? c.Role.ToString() : null,
                IsRevealed = c.IsRevealed
            }).ToList(),
            Clues = room.Clues.Select(clue => new ClueDto
            {
                Id = clue.Id,
                Word = clue.Word,
                Count = clue.Count,
                SpymasterTeam = clue.SpymasterTeam
            }).ToList(),
            ActionLogs = room.ActionLogs.Select(log => new ActionLogDto
            {
                Id = log.Id,
                PlayerName = log.PlayerName,
                Team = log.Team,
                Word = log.Word,
                CardRole = log.CardRole,
                Timestamp = log.Timestamp
            }).ToList()
        };
    }
}