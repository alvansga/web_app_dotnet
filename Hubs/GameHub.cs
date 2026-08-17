using Microsoft.AspNetCore.SignalR;
using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.GameEngine.Rules;

namespace WebAppSandbox.Hubs;

public class GameHub : Hub
{
    private readonly RoomManager _rooms;

    public GameHub(RoomManager rooms)
    {
        _rooms = rooms;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _rooms.RemoveConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // ---- Room lifecycle ----

    public async Task<string> CreateRoom(string playerName)
    {
        var roomId = _rooms.CreateRoom(Context.ConnectionId, playerName);
        var creator = _rooms.GetRoom(roomId).Engine.Game.Players[0];
        creator.ConnectionId = Context.ConnectionId;

        _rooms.RegisterConnection(Context.ConnectionId, roomId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Group(roomId).SendAsync("PlayerJoined", PlayerDto.From(_rooms.GetRoom(roomId).Engine.Game.Players[0]));
        await BroadcastPublicState(roomId);
        return roomId;
    }

    public async Task<string> JoinRoom(string roomId, string playerName)
    {
        var room = _rooms.JoinRoom(roomId, Context.ConnectionId, playerName);
        var player = room.Engine.Game.Players.First(p => p.Id == Context.ConnectionId);
        player.ConnectionId = Context.ConnectionId;

        _rooms.RegisterConnection(Context.ConnectionId, roomId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Group(roomId).SendAsync("PlayerJoined", PlayerDto.From(player));
        await BroadcastPublicState(roomId);
        return roomId;
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
    }

    // ---- Game actions ----

    public async Task DrawCard()
    {
        var room = GetCurrentRoom();

        try
        {
            room.Engine.DrawCard(Context.ConnectionId);
        }
        catch (GameRuleException ex)
        {
            throw new HubException(ex.Message);
        }

        await BroadcastPublicState(room.RoomId);
        await BroadcastHands(room);
    }

    public async Task PlayCard(string cardId, string targetOwnerPlayerId, string targetOrganType)
    {
        var room = GetCurrentRoom();

        if (!Enum.TryParse<OrganType>(targetOrganType, out var organType))
        {
            throw new HubException("Invalid target organ type.");
        }

        try
        {
            room.Engine.PlayCard(Context.ConnectionId, cardId, targetOwnerPlayerId, organType);
        }
        catch (GameRuleException ex)
        {
            throw new HubException(ex.Message);
        }

        await BroadcastPublicState(room.RoomId);
        await BroadcastHands(room);

        if (room.Engine.Game.Phase == GamePhase.GameOver)
        {
            var winnerId = room.Engine.Game.WinnerPlayerId;
            if (winnerId is not null)
            {
                await Clients.Group(room.RoomId).SendAsync("GameOver", winnerId);
            }
        }
    }

    public async Task SwapCards(IReadOnlyCollection<string> cardIds)
    {
        var room = GetCurrentRoom();

        try
        {
            room.Engine.SwapCards(Context.ConnectionId, cardIds);
        }
        catch (GameRuleException ex)
        {
            throw new HubException(ex.Message);
        }

        await BroadcastPublicState(room.RoomId);
        await BroadcastHands(room);
    }

    public async Task EndTurn()
    {
        var room = GetCurrentRoom();

        try
        {
            room.Engine.EndTurn(Context.ConnectionId);
        }
        catch (GameRuleException ex)
        {
            throw new HubException(ex.Message);
        }

        await BroadcastPublicState(room.RoomId);
        await BroadcastHands(room);
    }

    // ---- Helpers ----

    private GameRoom GetCurrentRoom()
    {
        var roomId = _rooms.GetRoomIdForConnection(Context.ConnectionId);

        if (roomId is null)
        {
            throw new HubException("You are not in a room.");
        }

        return _rooms.GetRoom(roomId);
    }

    private async Task BroadcastPublicState(string roomId)
    {
        var dto = BuildPublicState(_rooms.GetRoom(roomId));
        await Clients.Group(roomId).SendAsync("GameStateUpdate", dto);
    }

    private async Task BroadcastHands(GameRoom room)
    {
        foreach (var player in room.Engine.Game.Players)
        {
            if (player.ConnectionId is null)
            {
                continue;
            }

            var hand = player.Hand.Select(CardDto.From).ToList();
            await Clients.Client(player.ConnectionId).SendAsync("YourHand", hand);
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