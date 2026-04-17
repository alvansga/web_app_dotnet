namespace CodenameApp.Domain;

public class Player
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    public Guid? GameRoomId { get; set; }
    public GameRoom? GameRoom { get; set; }

    public Player(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }
}