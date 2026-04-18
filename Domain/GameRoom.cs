using System.Collections.Generic;
using System.Linq;

namespace CodenameApp.Domain;

public class GameRoom
{
    public Guid Id { get; private set; }
    public string Code { get; private set; }

    public List<Player> Players { get; private set; } = new();

    public RoomStatus Status { get; private set; } = RoomStatus.Waiting;
    public GameState State { get; set; } = new();


    public GameRoom(string code)
    {
        Id = Guid.NewGuid();
        Code = code;
    }

    public void AddPlayer(Player player)
    {
        if (Status != RoomStatus.Waiting)
            throw new Exception("Game already started");

        if (Players.Any(p => p.Id == player.Id))
            return;

        Players.Add(player);
    }

    public void StartGame()
    {
        if (Players.Count < 2)
            throw new Exception("Need at least 2 players to start");

        Status = RoomStatus.Playing;
    }
}


public enum RoomStatus
{
    Empty,
    Waiting,
    Playing,
    Finished
}

public class GameState
{

    public GamePhase Phase { get; set; } = GamePhase.Empty; // lobby, playing, ended
    public int Round { get; set; } = 0;
    public bool IsStarted { get; set; } = false;
}

public enum GamePhase
{
    Empty,
    Lobby,
    Playing,
    Ended
}