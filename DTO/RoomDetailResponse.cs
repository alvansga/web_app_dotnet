public class RoomDetailResponse
{
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = "Empty";

    public GameStateDto State { get; set; } = new();
    public Guid Id { get; set; }
    public List<PlayerDto> Players { get; set; } = new();
    public List<GameCardDto> Cards { get; set; } = new();
    public List<ClueDto> Clues { get; set; } = new();
    public List<ActionLogDto> ActionLogs { get; set; } = new();
}

public class PlayerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GameRole { get; set; } = "None";
}

public class GameStateDto
{
    public string Phase { get; set; } = "Empty";
    public int Round { get; set; } = 0;
    public bool IsStarted { get; set; } = false;
    public string? Winner { get; set; }
    public string? CurrentTurn { get; set; }
}

/// <summary>Field operative: hanya tahu apakah kartu sudah direveal, tidak tahu role-nya (kecuali sudah reveal)</summary>
public class GameCardDto
{
    public Guid Id { get; set; }
    public string Word { get; set; } = string.Empty;
    /// <summary>Null jika belum direveal dan requester bukan spymaster</summary>
    public string? Role { get; set; }
    public bool IsRevealed { get; set; }
}

public class ClueDto
{
    public Guid Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public int Count { get; set; }
    public string SpymasterTeam { get; set; } = "Unknown";
}

// ====== Request DTOs ======
public class AssignRoleRequest
{
    public Guid PlayerId { get; set; }
    public string Token { get; set; } = string.Empty;
}

public class RevealCardRequest
{
    public Guid PlayerId { get; set; }
    public string Token { get; set; } = string.Empty;
}

public class StartGameRequest
{
    public Guid PlayerId { get; set; }
    public string Token { get; set; } = string.Empty;
}

public class LeaveRoomRequest
{
    public Guid PlayerId { get; set; }
    public string Token { get; set; } = string.Empty;
}

public class AddClueRequest
{
    public Guid PlayerId { get; set; }
    public string Word { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Token { get; set; } = string.Empty;
}

public class ActionLogDto
{
    public Guid Id { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public string Word { get; set; } = string.Empty;
    public string CardRole { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}