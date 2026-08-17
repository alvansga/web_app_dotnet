using Microsoft.AspNetCore.SignalR;
using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.GameEngine.Rules;

namespace WebAppSandbox.Hubs;

public class GameHub : Hub
{
    private readonly RoomManager _rooms;
    private readonly IHubContext<GameHub> _hubContext;

    public GameHub(RoomManager rooms, IHubContext<GameHub> hubContext)
    {
        _rooms = rooms;
        _hubContext = hubContext;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var roomId = _rooms.GetRoomIdForConnection(Context.ConnectionId);
        if (roomId is not null && _rooms.TryGetRoom(roomId, out var room))
        {
            var player = room!.Engine.Game.Players
                .FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player is not null)
            {
                player.ConnectionId = null;
            }
        }

        _rooms.RemoveConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // ---- Room lifecycle ----

    public async Task<string> CreateRoom(string playerId, string playerName)
    {
        var roomId = _rooms.CreateRoom(playerId, playerName);
        var creator = _rooms.GetRoom(roomId).Engine.Game.Players[0];
        creator.ConnectionId = Context.ConnectionId;

        _rooms.RegisterConnection(Context.ConnectionId, roomId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Group(roomId).SendAsync("PlayerJoined", PlayerDto.From(_rooms.GetRoom(roomId).Engine.Game.Players[0]));
        await BroadcastPublicState(roomId);
        return roomId;
    }

    public async Task<string> JoinRoom(string roomId, string playerId, string playerName)
    {
        var room = _rooms.JoinRoom(roomId, playerId, playerName);
        var player = room.Engine.Game.Players.First(p => p.Id == playerId);
        player.ConnectionId = Context.ConnectionId;

        _rooms.RegisterConnection(Context.ConnectionId, roomId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Group(roomId).SendAsync("PlayerJoined", PlayerDto.From(player));
        await BroadcastPublicState(roomId);
        return roomId;
    }

    public async Task RejoinRoom(string roomId, string playerId)
    {
        if (!_rooms.TryGetRoom(roomId, out var room))
        {
            throw new HubException("Room not found.");
        }

        var player = room!.Engine.Game.Players.FirstOrDefault(p => p.Id == playerId);
        if (player is null)
        {
            throw new HubException("You are not in this room.");
        }

        player.ConnectionId = Context.ConnectionId;
        _rooms.RegisterConnection(Context.ConnectionId, room.RoomId);
        await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomId);

        await BroadcastPublicState(room.RoomId);
        await BroadcastHands(room);
    }

    public Task<List<RoomDto>> ListRooms()
    {
        var rooms = _rooms.GetOpenRooms()
            .Select(RoomDto.From)
            .ToList();

        return Task.FromResult(rooms);
    }

    public async Task StartGame()
    {
        var room = GetCurrentRoom();

        try
        {
            room.Engine.StartGame(new Random());
        }
        catch (GameRuleException ex)
        {
            throw new HubException(ex.Message);
        }

        await BroadcastPublicState(room.RoomId);
        await BroadcastHands(room);

        await BroadcastAction(room.RoomId,
            $"Permainan dimulai. Giliran {CurrentTurnName(room.Engine.Game)}.");
    }

    // ---- Game actions ----

    public async Task DrawCard()
    {
        var room = GetCurrentRoom();
        var playerId = CurrentPlayerId(room);

        try
        {
            room.Engine.DrawCard(playerId);
        }
        catch (GameRuleException ex)
        {
            throw new HubException(ex.Message);
        }

        var drawActor = PlayerName(room.Engine.Game, playerId);
        await BroadcastAction(room.RoomId,
            $"{drawActor} menarik kartu. Giliran {CurrentTurnName(room.Engine.Game)}.");

        await BroadcastPublicState(room.RoomId);
        await BroadcastHands(room);
    }

    public async Task PlayCard(string cardId, string targetOwnerPlayerId, string targetOrganType)
    {
        var room = GetCurrentRoom();
        var playerId = CurrentPlayerId(room);

        if (!Enum.TryParse<OrganType>(targetOrganType, out var organType))
        {
            throw new HubException("Invalid target organ type.");
        }

        var actor = playerId;
        var cardName = cardId;
        var cardType = "Kartu";
        var targetName = targetOwnerPlayerId;

        try
        {
            var game = room.Engine.Game;
            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            var card = player?.Hand.FirstOrDefault(c => c.Id == cardId);

            actor = player?.Name ?? playerId;
            cardName = card?.Name ?? cardId;
            cardType = card?.Type.ToString() ?? "Kartu";
            targetName = PlayerName(game, targetOwnerPlayerId);

            room.Engine.PlayCard(playerId, cardId, targetOwnerPlayerId, organType);
        }
        catch (GameRuleException ex)
        {
            throw new HubException(ex.Message);
        }

        await BroadcastPublicState(room.RoomId);
        await BroadcastHands(room);

        var pending = room.Engine.Game.PendingAttack;
        if (pending is not null)
        {
            await Clients.Group(room.RoomId).SendAsync("PendingAttack",
                PendingAttackDto.From(pending, GameRules.ResponseTimeoutSeconds));

            await BroadcastAction(room.RoomId,
                $"{actor} meluncurkan {cardName} ({cardType}) ke {targetOrganType} milik {targetName}. Menunggu respons...");

            SchedulePendingAttackResolution(room.RoomId, pending.Id);
        }
        else
        {
            await BroadcastAction(room.RoomId,
                $"{actor} memainkan {cardName} ({cardType}) ke {targetOrganType} milik {targetName}. Giliran {CurrentTurnName(room.Engine.Game)}.");
        }

        if (room.Engine.Game.Phase == GamePhase.GameOver)
        {
            var winnerId = room.Engine.Game.WinnerPlayerId;
            if (winnerId is not null)
            {
                await Clients.Group(room.RoomId).SendAsync("GameOver", winnerId);
            }
        }
    }

    public async Task PlayNoTargetCard(string cardId)
    {
        var room = GetCurrentRoom();
        var playerId = CurrentPlayerId(room);

        var actor = PlayerName(room.Engine.Game, playerId);
        var cardName = room.Engine.Game.Players
            .FirstOrDefault(p => p.Id == playerId)?
            .Hand.FirstOrDefault(c => c.Id == cardId)?.Name ?? cardId;

        try
        {
            room.Engine.PlayNoTargetCard(playerId, cardId);
        }
        catch (GameRuleException ex)
        {
            throw new HubException(ex.Message);
        }

        await BroadcastPublicState(room.RoomId);
        await BroadcastHands(room);

        await BroadcastAction(room.RoomId,
            $"{actor} memainkan {cardName}. Giliran {CurrentTurnName(room.Engine.Game)}.");
    }

    public async Task PlayInstant(string cardId)
    {
        var room = GetCurrentRoom();
        var playerId = CurrentPlayerId(room);

        try
        {
            room.Engine.PlayInstant(playerId, cardId);
        }
        catch (GameRuleException ex)
        {
            throw new HubException(ex.Message);
        }

        await BroadcastPublicState(room.RoomId);
        await BroadcastHands(room);

        await BroadcastAction(room.RoomId,
            $"{PlayerName(room.Engine.Game, playerId)} memblokir serangan dengan Immunity Boost! Giliran {CurrentTurnName(room.Engine.Game)}.");
    }

    public async Task SwapCards(IReadOnlyCollection<string> cardIds)
    {
        var room = GetCurrentRoom();
        var playerId = CurrentPlayerId(room);

        try
        {
            room.Engine.SwapCards(playerId, cardIds);
        }
        catch (GameRuleException ex)
        {
            throw new HubException(ex.Message);
        }

        var swapActor = PlayerName(room.Engine.Game, playerId);
        await BroadcastAction(room.RoomId,
            $"{swapActor} menukar {cardIds.Count} kartu. Giliran {CurrentTurnName(room.Engine.Game)}.");

        await BroadcastPublicState(room.RoomId);
        await BroadcastHands(room);
    }

    public async Task EndTurn()
    {
        var room = GetCurrentRoom();
        var playerId = CurrentPlayerId(room);

        try
        {
            room.Engine.EndTurn(playerId);
        }
        catch (GameRuleException ex)
        {
            throw new HubException(ex.Message);
        }

        var endActor = PlayerName(room.Engine.Game, playerId);
        await BroadcastAction(room.RoomId,
            $"{endActor} mengakhiri giliran. Giliran {CurrentTurnName(room.Engine.Game)}.");

        await BroadcastPublicState(room.RoomId);
        await BroadcastHands(room);
    }

    // ---- Helpers ----

    private async Task BroadcastAction(string roomId, string message)
    {
        await Clients.Group(roomId).SendAsync("ActionLog", message);
    }

    private static string PlayerName(Game game, string playerId)
    {
        return game.Players.FirstOrDefault(p => p.Id == playerId)?.Name ?? playerId;
    }

    private string CurrentTurnName(Game game)
    {
        return game.Turn is null ? "-" : PlayerName(game, game.Turn.CurrentPlayerId);
    }

    private GameRoom GetCurrentRoom()
    {
        var roomId = _rooms.GetRoomIdForConnection(Context.ConnectionId);

        if (roomId is null)
        {
            throw new HubException("You are not in a room.");
        }

        return _rooms.GetRoom(roomId);
    }

    private string CurrentPlayerId(GameRoom room)
    {
        var player = room.Engine.Game.Players
            .FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

        if (player is null)
        {
            throw new HubException("You are not in a room.");
        }

        // Self-heal the connection mapping in case it was lost during reconnect.
        _rooms.RegisterConnection(Context.ConnectionId, room.RoomId);

        return player.Id;
    }

    private async Task BroadcastPublicState(string roomId)
    {
        var dto = BuildPublicState(_rooms.GetRoom(roomId));
        await Clients.Group(roomId).SendAsync("GameStateUpdate", dto);
    }

    private async Task BroadcastHands(GameRoom room)
    {
        await BroadcastHandsTo(room, _hubContext.Clients);
    }

    private static async Task BroadcastHandsTo(GameRoom room, IHubClients clients)
    {
        foreach (var player in room.Engine.Game.Players)
        {
            if (player.ConnectionId is null)
            {
                continue;
            }

            var hand = player.Hand.Select(CardDto.From).ToList();
            await clients.Client(player.ConnectionId).SendAsync("YourHand", hand);
        }
    }

    private void SchedulePendingAttackResolution(string roomId, string pendingId)
    {
        var hubContext = _hubContext;
        var rooms = _rooms;

        _ = ResolveAfterDelayAsync(roomId, pendingId, hubContext, rooms);
    }

    private static async Task ResolveAfterDelayAsync(
        string roomId,
        string pendingId,
        IHubContext<GameHub> hubContext,
        RoomManager rooms)
    {
        await Task.Delay(TimeSpan.FromSeconds(GameRules.ResponseTimeoutSeconds));

        var room = rooms.GetRoom(roomId);
        var game = room.Engine.Game;

        // If the defender already responded, the pending attack is gone; do nothing.
        if (game.PendingAttack?.Id != pendingId)
        {
            return;
        }

        var pending = game.PendingAttack;
        room.Engine.ResolvePendingAttack(pendingId, blocked: false);

        var clients = hubContext.Clients;
        await clients.Group(roomId).SendAsync("GameStateUpdate", BuildPublicState(room));
        await BroadcastHandsTo(room, clients);

        await clients.Group(roomId).SendAsync("ActionLog",
            $"{pending.Caster.Name} mengenai {pending.TargetOrgan.Type} milik {pending.TargetOwner.Name}.");

        if (game.Phase == GamePhase.GameOver && game.WinnerPlayerId is not null)
        {
            await clients.Group(roomId).SendAsync("GameOver", game.WinnerPlayerId);
        }
    }

    private static GameStateDto BuildPublicState(GameRoom room)
    {
        var game = room.Engine.Game;

        var turn = game.Turn is null
            ? null
            : new TurnDto
            {
                CurrentPlayerId = game.Turn.CurrentPlayerId,
                ActionsUsed = game.Turn.ActionsUsed,
                MaxActionsPerTurn = game.Turn.MaxActionsPerTurn,
                HasDrawnThisTurn = game.Turn.HasDrawnThisTurn,
            };

        return new GameStateDto
        {
            RoomId = room.RoomId,
            Phase = game.Phase.ToString(),
            WinnerPlayerId = game.WinnerPlayerId,
            Turn = turn,
            Players = game.Players.Select(PlayerDto.From).ToList(),
            DeckCount = game.Deck?.DrawPile.Count ?? 0,
            DiscardCount = game.Deck?.DiscardPile.Count ?? 0,
        };
    }
}