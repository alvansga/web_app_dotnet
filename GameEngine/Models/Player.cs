namespace WebAppSandbox.GameEngine.Models;

public class Player
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string? ConnectionId { get; set; }
    public List<Organ> Organs { get; } = new();
    public List<Card> Hand { get; } = new();

    public bool IsAlive => Organs.Any(o => !o.IsDestroyed);

    public Player(string id, string name)
    {
        Id = id;
        Name = name;
    }
}