using System.Collections.Generic;
using System.Linq;

namespace CodenameApp.Domain;

public class GameRoom
{
    public Guid Id { get; private set; }
    public string Code { get; private set; }

    public List<Player> Players { get; private set; } = new();

    public GameRoom(string code)
    {
        Id = Guid.NewGuid();
        Code = code;
    }

    public void AddPlayer(Player player)
    {
        if (Players.Any(p => p.Id == player.Id))
            return;

        Players.Add(player);
    }
}