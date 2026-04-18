using CodenameApp.Domain;

public class RoomDetailResponse
{
    public string Code { get; set; } = string.Empty;
    public RoomStatus Status { get; set; } = RoomStatus.Empty;

    public GameStateDto State { get; set; } = new();
    public Guid Id { get; set; }
    public List<PlayerDto> Players { get; set; } = new();
    public List<GameCardDto> Cards { get; set; } = new();
}

public class PlayerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class GameStateDto
{
    public GamePhase Phase { get; set; }
    public int Round { get; set; } = 0;
    public bool IsStarted { get; set; } = false;
}

public class GameCardDto
{
    public Guid Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public CardRole Role { get; set; }
    public bool IsRevealed { get; set; }
}