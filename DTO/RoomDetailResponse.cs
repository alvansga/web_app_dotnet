public class RoomDetailResponse
{
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public List<PlayerDto> Players { get; set; } = new();
}

public class PlayerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}