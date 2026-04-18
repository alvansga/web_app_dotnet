public class PlayerResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? GameRoomId { get; set; }
}