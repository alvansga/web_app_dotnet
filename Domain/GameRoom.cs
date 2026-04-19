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
    
    public List<GameCard> Cards { get; private set; } = new();


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

    public void StartGame(IEnumerable<string> words)
    {
        if (Players.Count < 2)
            throw new Exception("Need at least 2 players to start");

        var wordList = words.ToList();
        if (wordList.Count < 25)
            throw new Exception("Need exactly 25 words to start the game");

        Status = RoomStatus.Playing;
        State.Phase = GamePhase.SpymasterSelection;
        State.IsStarted = true;
        State.Round = 1;

        // Determine starting team randomly
        var random = new Random();
        bool isRedStart = random.Next(2) == 0;

        // Roles distribution: 1 Assassin, 7 Bystander, (9 for start team, 8 for other team)
        int redCount = isRedStart ? 9 : 8;
        int blueCount = isRedStart ? 8 : 9;
        
        var roles = new List<CardRole>();
        roles.Add(CardRole.Assassin);
        roles.AddRange(Enumerable.Repeat(CardRole.Bystander, 7));
        roles.AddRange(Enumerable.Repeat(CardRole.RedAgent, redCount));
        roles.AddRange(Enumerable.Repeat(CardRole.BlueAgent, blueCount));

        // Shuffle roles
        roles = roles.OrderBy(x => random.Next()).ToList();

        // Create cards
        Cards.Clear();
        for (int i = 0; i < 25; i++)
        {
            Cards.Add(new GameCard(wordList[i], roles[i]));
        }
    }

    public void AssignSpymaster(Guid playerId)
    {
        if (Status != RoomStatus.Playing)
            throw new Exception("Game has not started");

        var player = Players.FirstOrDefault(p => p.Id == playerId)
            ?? throw new Exception("Player not found in this room");

        player.SetGameRole(PlayerGameRole.Spymaster);
        
        // Check if all players have chosen a role -> move to Playing phase
        if (Players.All(p => p.GameRole != PlayerGameRole.None))
            State.Phase = GamePhase.Playing;
    }

    public void AssignFieldOperative(Guid playerId)
    {
        if (Status != RoomStatus.Playing)
            throw new Exception("Game has not started");

        var player = Players.FirstOrDefault(p => p.Id == playerId)
            ?? throw new Exception("Player not found in this room");

        player.SetGameRole(PlayerGameRole.FieldOperative);

        if (Players.All(p => p.GameRole != PlayerGameRole.None))
            State.Phase = GamePhase.Playing;
    }

    public void RevealCard(Guid cardId, Guid requestingPlayerId)
    {
        var player = Players.FirstOrDefault(p => p.Id == requestingPlayerId)
            ?? throw new Exception("Player not found in this room");

        if (player.GameRole != PlayerGameRole.FieldOperative)
            throw new Exception("Only field operatives can reveal cards");

        var card = Cards.FirstOrDefault(c => c.Id == cardId)
            ?? throw new Exception("Card not found");

        if (card.IsRevealed)
            throw new Exception("Card already revealed");

        card.Reveal();
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
    SpymasterSelection,
    Playing,
    Ended
}

public enum CardRole
{
    Assassin,
    RedAgent,
    BlueAgent,
    Bystander
}

public enum PlayerGameRole
{
    None,
    Spymaster,
    FieldOperative
}

public class GameCard
{
    public Guid Id { get; private set; }
    public string Word { get; private set; }
    public CardRole Role { get; private set; }
    public bool IsRevealed { get; private set; } = false;

    // Foreign Key mapping back to GameRoom
    public Guid GameRoomId { get; private set; }

    // For EF Core
    private GameCard() { }

    public GameCard(string word, CardRole role)
    {
        Id = Guid.NewGuid();
        Word = word;
        Role = role;
    }

    public void Reveal() => IsRevealed = true;
}