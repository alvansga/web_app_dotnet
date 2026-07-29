public class JoinRoomRequest
{
    public string Code { get; set; } = string.Empty;
    public Guid PlayerId { get; set; }
    public string Token { get; set; } = string.Empty;
}