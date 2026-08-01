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
        if (Status != RoomStatus.Waiting && Status != RoomStatus.Finished)
            throw new Exception("Game already started");

        if (Players.Any(p => p.Id == player.Id))
            return;

        Players.Add(player);
    }

    public void StartGame(IEnumerable<string> words, Guid starterId)
    {
        if (Players.Count < 2)
            throw new Exception("Need at least 2 players to start");

        // Validate: must have at least one Spymaster (either Red or Blue)
        bool hasRedSpymaster = Players.Any(p => p.GameRole == PlayerGameRole.RedSpymaster);
        bool hasBlueSpymaster = Players.Any(p => p.GameRole == PlayerGameRole.BlueSpymaster);

        if (!hasRedSpymaster && !hasBlueSpymaster)
            throw new Exception("Need at least one Spymaster to start");

        // Assign any remaining None players to field operative of their team based on order
        var redPlayers = Players.Where(p => p.GameRole == PlayerGameRole.RedSpymaster || p.GameRole == PlayerGameRole.RedFieldOperative).ToList();
        var bluePlayers = Players.Where(p => p.GameRole == PlayerGameRole.BlueSpymaster || p.GameRole == PlayerGameRole.BlueFieldOperative).ToList();
        var unassigned = Players.Where(p => p.GameRole == PlayerGameRole.None).ToList();

        foreach (var p in unassigned)
        {
            if (redPlayers.Count <= bluePlayers.Count)
            {
                p.SetGameRole(PlayerGameRole.RedFieldOperative);
                redPlayers.Add(p);
            }
            else
            {
                p.SetGameRole(PlayerGameRole.BlueFieldOperative);
                bluePlayers.Add(p);
            }
        }

        var wordList = words.ToList();
        if (wordList.Count < 25)
            throw new Exception("Need exactly 25 words to start the game");

        Status = RoomStatus.Playing;
        State.Phase = GamePhase.Playing;
        State.IsStarted = true;
        State.Round = 1;
        State.Winner = null;

        // Determine starting team randomly
        var random = new Random();
        bool isRedStart = random.Next(2) == 0;
        State.CurrentTurn = isRedStart ? "Red" : "Blue";

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
        Clues.Clear();
        for (int i = 0; i < 25; i++)
        {
            Cards.Add(new GameCard(Id, wordList[i], roles[i]));
        }
    }

    public void AssignRole(Guid playerId, PlayerGameRole role)
    {
        if (Status != RoomStatus.Waiting && Status != RoomStatus.Finished)
            throw new Exception("Game has already started, cannot change role");

        var player = Players.FirstOrDefault(p => p.Id == playerId)
            ?? throw new Exception("Player not found in this room");

        player.SetGameRole(role);
    }

    public void RevealCard(Guid cardId, Guid requestingPlayerId)
    {
        if (State.Phase != GamePhase.Playing)
            throw new Exception("Cannot reveal cards in this phase.");

        var player = Players.FirstOrDefault(p => p.Id == requestingPlayerId)
            ?? throw new Exception("Player not found in this room");

        // Check player is a field operative
        if (player.GameRole != PlayerGameRole.RedFieldOperative &&
            player.GameRole != PlayerGameRole.BlueFieldOperative &&
            player.GameRole != PlayerGameRole.FieldOperative)
            throw new Exception("Only field operatives can reveal cards");

        // Check it's their team's turn
        string? playerTeam = GetPlayerTeam(player);
        if (playerTeam != null && State.CurrentTurn != null && playerTeam != State.CurrentTurn)
            throw new Exception($"It's not your team's turn! Current turn: {State.CurrentTurn} team");

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
        
        if (player.GameRole != PlayerGameRole.RedSpymaster &&
            player.GameRole != PlayerGameRole.BlueSpymaster &&
            player.GameRole != PlayerGameRole.Spymaster)
            throw new Exception("Only spymasters can set clues");

        if (string.IsNullOrWhiteSpace(word) || word.Trim().Contains(" "))
            throw new Exception("Clue must be a single word");

        string spymasterTeam = "Unknown";
        if (player.GameRole == PlayerGameRole.RedSpymaster)
            spymasterTeam = "Red";
        else if (player.GameRole == PlayerGameRole.BlueSpymaster)
            spymasterTeam = "Blue";

        Clues.Add(new Clue(Id, word.Trim().ToUpper(), count, spymasterTeam));
    }

    public void RemoveClue(Guid clueId, Guid spymasterId)
    {
        var player = Players.FirstOrDefault(p => p.Id == spymasterId)
            ?? throw new Exception("Player not found");
        
        if (player.GameRole != PlayerGameRole.RedSpymaster &&
            player.GameRole != PlayerGameRole.BlueSpymaster &&
            player.GameRole != PlayerGameRole.Spymaster)
            throw new Exception("Only spymasters can remove clues");

        var clue = Clues.FirstOrDefault(c => c.Id == clueId);
        if (clue != null)
        {
            Clues.Remove(clue);
        }
    }

    private string? GetPlayerTeam(Player player)
    {
        if (player.GameRole == PlayerGameRole.RedSpymaster || player.GameRole == PlayerGameRole.RedFieldOperative)
            return "Red";
        if (player.GameRole == PlayerGameRole.BlueSpymaster || player.GameRole == PlayerGameRole.BlueFieldOperative)
            return "Blue";
        return null;
    }
}

public class Clue
{
    public Guid Id { get; private set; }
    public string Word { get; private set; }
    public int Count { get; private set; }
    public string SpymasterTeam { get; private set; }
    
    public Guid GameRoomId { get; private set; }
    public GameRoom? GameRoom { get; private set; }

    public Clue(Guid gameRoomId, string word, int count, string spymasterTeam = "Unknown")
    {
        Id = Guid.NewGuid();
        GameRoomId = gameRoomId;
        Word = word;
        Count = count;
        SpymasterTeam = spymasterTeam;
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
    /// <summary>"Red" | "Blue" | null — current turn</summary>
    public string? CurrentTurn { get; set; }
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
    FieldOperative,
    RedSpymaster,
    BlueSpymaster,
    RedFieldOperative,
    BlueFieldOperative
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