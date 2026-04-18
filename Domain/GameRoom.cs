using System.Collections.Generic;
using System.Linq;

namespace CodenameApp.Domain;

public class GameRoom
{
    public Guid Id { get; private set; }
    public string Code { get; private set; }

    public List<Player> Players { get; private set; } = new();
    public string Status { get; set; } = "waiting"; // waiting, playing, finished


    public GameRoom(string code)
    {
        Id = Guid.NewGuid();
        Code = code;
    }

    public void AddPlayer(Player player)
    {
        if (Status != "waiting")
            throw new Exception("Game already started");

        if (Players.Any(p => p.Id == player.Id))
            return;

        Players.Add(player);
    }

    public void StartGame()
    {
        if (Players.Count < 2)
            throw new Exception("Need at least 2 players to start");

        Status = "playing";
    }
}