namespace CodenameApp.Domain;

public class Player
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Token { get; private set; }

    public Guid? GameRoomId { get; set; }
    public GameRoom? GameRoom { get; set; }

    public PlayerGameRole GameRole { get; private set; } = PlayerGameRole.None;

    // For EF Core
    private Player() { }

    public Player(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        Token = Guid.NewGuid().ToString("N");
    }

    public void SetGameRole(PlayerGameRole role)
    {
        GameRole = role;
    }

    public void LeaveRoom()
    {
        GameRoomId = null;
        GameRoom = null;
        GameRole = PlayerGameRole.None;
    }
}