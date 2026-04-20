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
    public List<Clue> Clues { get; private set; } = new();


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

    public void StartGame(IEnumerable<string> words, Guid starterId)
    {
        if (Players.Count < 2)
            throw new Exception("Need at least 2 players to start");

        var wordList = words.ToList();
        if (wordList.Count < 25)
            throw new Exception("Need exactly 25 words to start the game");

        // Assign roles immediately
        foreach (var p in Players)
        {
            if (p.Id == starterId)
                p.SetGameRole(PlayerGameRole.Spymaster);
            else
                p.SetGameRole(PlayerGameRole.FieldOperative);
        }

        Status = RoomStatus.Playing;
        State.Phase = GamePhase.Playing; // Langsung Phase Playing
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
            Cards.Add(new GameCard(Id, wordList[i], roles[i]));
        }
    }

    public void AssignSpymaster(Guid playerId)
    {
        if (Status != RoomStatus.Playing)
            throw new Exception("Game has not started");

        // Limit to 1 Spymaster (as per user's focused request)
        if (Players.Any(p => p.GameRole == PlayerGameRole.Spymaster))
            throw new Exception("Spymaster role is already taken.");

        var player = Players.FirstOrDefault(p => p.Id == playerId)
            ?? throw new Exception("Player not found in this room");

        player.SetGameRole(PlayerGameRole.Spymaster);
        
        // AUTO-START: Once we have a spymaster, the game is ready for field ops
        State.Phase = GamePhase.Playing;
    }

    public void AssignFieldOperative(Guid playerId)
    {
        if (Status != RoomStatus.Playing)
            throw new Exception("Game has not started");

        var player = Players.FirstOrDefault(p => p.Id == playerId)
            ?? throw new Exception("Player not found in this room");

        player.SetGameRole(PlayerGameRole.FieldOperative);

        // If at least one spymaster exists, we can be in Playing phase
        if (Players.Any(p => p.GameRole == PlayerGameRole.Spymaster))
            State.Phase = GamePhase.Playing;
    }

    public void RevealCard(Guid cardId, Guid requestingPlayerId)
    {
        // Force transition to Playing if someone starts guessing
        if (State.Phase == GamePhase.SpymasterSelection)
            State.Phase = GamePhase.Playing;

        if (State.Phase != GamePhase.Playing)
            throw new Exception("Cannot reveal cards in this phase.");

        var player = Players.FirstOrDefault(p => p.Id == requestingPlayerId)
            ?? throw new Exception("Player not found in this room");

        if (player.GameRole != PlayerGameRole.FieldOperative)
            throw new Exception("Only field operatives can reveal cards");

        var card = Cards.FirstOrDefault(c => c.Id == cardId)
            ?? throw new Exception("Card not found");

        if (card.IsRevealed)
            throw new Exception("Card already revealed");

        card.Reveal();

        // === Win/Lose condition checks ===
        if (card.Role == CardRole.Assassin)
        {
            Status = RoomStatus.Finished;
            State.Phase = GamePhase.Ended;
            State.Winner = "Assassin";
            return;
        }

        bool allRedRevealed = Cards
            .Where(c => c.Role == CardRole.RedAgent)
            .All(c => c.IsRevealed);

        bool allBlueRevealed = Cards
            .Where(c => c.Role == CardRole.BlueAgent)
            .All(c => c.IsRevealed);

        if (allRedRevealed)
        {
            Status = RoomStatus.Finished;
            State.Phase = GamePhase.Ended;
            State.Winner = "RedTeam";
        }
        else if (allBlueRevealed)
        {
            Status = RoomStatus.Finished;
            State.Phase = GamePhase.Ended;
            State.Winner = "BlueTeam";
        }
    }

    public void AddClue(string word, int count, Guid spymasterId)
    {
        var player = Players.FirstOrDefault(p => p.Id == spymasterId)
            ?? throw new Exception("Player not found");
        
        if (player.GameRole != PlayerGameRole.Spymaster)
            throw new Exception("Only spymasters can set clues");

        if (string.IsNullOrWhiteSpace(word) || word.Trim().Contains(" "))
            throw new Exception("Clue must be a single word");

        Clues.Add(new Clue(Id, word.Trim().ToUpper(), count));
    }

    public void RemoveClue(Guid clueId, Guid spymasterId)
    {
        var player = Players.FirstOrDefault(p => p.Id == spymasterId)
            ?? throw new Exception("Player not found");
        
        if (player.GameRole != PlayerGameRole.Spymaster)
            throw new Exception("Only spymasters can remove clues");

        var clue = Clues.FirstOrDefault(c => c.Id == clueId);
        if (clue != null)
        {
            Clues.Remove(clue);
        }
    }
}

public class Clue
{
    public Guid Id { get; private set; }
    public string Word { get; private set; }
    public int Count { get; private set; }
    
    public Guid GameRoomId { get; private set; }
    public GameRoom? GameRoom { get; private set; }

    public Clue(Guid gameRoomId, string word, int count)
    {
        Id = Guid.NewGuid();
        GameRoomId = gameRoomId;
        Word = word;
        Count = count;
    }

    private Clue() { } // For EF Core
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
    public GamePhase Phase { get; set; } = GamePhase.Empty;
    public int Round { get; set; } = 0;
    public bool IsStarted { get; set; } = false;
    /// <summary>"RedTeam" | "BlueTeam" | "Assassin" | null (game not finished)</summary>
    public string? Winner { get; set; }
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
    public GameRoom? GameRoom { get; private set; }

    // For EF Core
    private GameCard() { }

    public GameCard(Guid gameRoomId, string word, CardRole role)
    {
        Id = Guid.NewGuid();
        GameRoomId = gameRoomId;
        Word = word;
        Role = role;
    }

    public void Reveal() => IsRevealed = true;
}