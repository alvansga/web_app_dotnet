using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System;
using System.Timers;
using WebAppSandbox.Models;

namespace WebAppSandbox.Hubs
{
    public class DrawingHub : Hub
    {
        // Multi-room management
        private static readonly ConcurrentDictionary<string, GameState> _rooms = new();
        private static readonly ConcurrentDictionary<string, string> _userRooms = new();
        
        private readonly IHubContext<DrawingHub> _hubContext;
        private static readonly Random _random = new();

        public DrawingHub(IHubContext<DrawingHub> hubContext)
        {
            _hubContext = hubContext;
        }

        private static readonly string[] _words = {
            "IRON MAN", "SPIDER-MAN", "THOR", "HULK", "BLACK WIDOW", "CAPTAIN AMERICA", "GROOT", "THANOS", "LOKI", "WOLVERINE",
            "BATMAN", "SUPERMAN", "WONDER WOMAN", "THE FLASH", "AQUAMAN", "JOKER", "HARLEY QUINN", "BLACK PANTHER", "DOCTOR STRANGE", "VENOM",
            "MICKEY MOUSE", "DONALD DUCK", "GOOFY", "ELSA", "ANNA", "OLAF", "SIMBA", "ALADDIN", "GENIE", "ARIEL",
            "MULAN", "STITCH", "BAYMAX", "WINNIE THE POOH", "MALEFICENT", "PETER PAN", "HERCULES", "MOANA", "MAUI", "RAPUNZEL",
            "WOODY", "BUZZ LIGHTYEAR", "NEMO", "DORY", "WALL-E", "EVE", "REMY", "SULLY", "MIKE WAZOWSKI", "LIGHTNING MCQUEEN", "TOW MATER",
            "MR. INCREDIBLE", "ELASTIGIRL", "JOY", "SADNESS", "BING BONG", "RUSSELL", "CARL FREDRICKSEN",
            "NARUTO", "SASUKE", "KAKASHI", "ITACHI", "GAARA", "KURAMA", "HINATA", "MADARA", "TSUNADE", "JIRAIYA",
            "LUFFY", "ZORO", "NAMI", "SANJI", "CHOPPER", "ROBIN", "BROOK", "SHANKS", "ACE", "KAIDO",
            "GOKU", "VEGETA", "FRIEZA", "CELL", "MAJIN BUU", "PIKACHU", "CHARIZARD", "DORAEMON", "TOTORO", "SAITAMA",
            "TANJIRO", "NEZUKO", "ZENITSU", "INOSUKE", "MUZAN", "RENGOKU", "LIGHT YAGAMI", "RYUK", "EDWARD ELRIC", "ALPHONSE ELRIC",
            "DEKU", "ALL MIGHT", "BAKUGO", "TODOROKI", "URARAKA", "IIDA", "FROPPY", "KIRISHIMA", "ENDEAVOR", "ERASERHEAD",
            "SHIGARAKI", "TOGA", "DABI", "ALL FOR ONE", "MIRIO",
            "EREN YEAGER", "MIKASA ACKERMAN", "LEVI ACKERMAN", "ARMIN ARLERT", "ERWIN SMITH", "REINER BRAUN", "BERTHOLDT", "ZEKE YEAGER",
            "COLOSSAL TITAN", "ARMORED TITAN",
            "SPONGEBOB", "PATRICK STAR", "SQUIDWARD", "MR. KRABS", "SANDY CHEEKS", "PLANKTON", "GARY THE SNAIL",
            "AANG", "KATARA", "SOKKA", "ZUKO", "TOPH", "APPA", "MOMO", "KORRA",
            "DANNY PHANTOM", "TIMMY TURNER", "COSMO", "WANDA", "JIMMY NEUTRON", "ARNOLD SHORTMAN", "CATDOG",
            "BEN 10", "GWEN TENNYSON", "KEVIN LEVIN", "BLOSSOM", "BUBBLES", "BUTTERCUP",
        };

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_userRooms.TryRemove(Context.ConnectionId, out string? roomCode))
            {
                if (_rooms.TryGetValue(roomCode, out var room))
                {
                    lock (room.Lock)
                    {
                        room.PlayerCount--;
                        if (room.PlayerCount <= 0)
                        {
                            // Self-destruct empty room to save memory
                            _rooms.TryRemove(roomCode, out _);
                        }
                    }
                }
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public string CreateRoom()
        {
            string roomCode;
            do
            {
                roomCode = new string(Enumerable.Repeat("ABCDEFGHJKLMNPQRSTUVWXYZ23456789", 6)
                    .Select(s => s[_random.Next(s.Length)]).ToArray());
            } while (_rooms.ContainsKey(roomCode));

            _rooms.TryAdd(roomCode, new GameState { RoomCode = roomCode });
            return roomCode;
        }

        public async Task<bool> JoinRoom(string roomCode, string playerName)
        {
            roomCode = roomCode?.ToUpper().Trim() ?? "";
            if (!_rooms.TryGetValue(roomCode, out var room)) return false;

            // Leave old room if any and update player count
            if (_userRooms.TryGetValue(Context.ConnectionId, out var oldRoomCode) && _rooms.TryGetValue(oldRoomCode, out var oldRoom))
            {
                lock (oldRoom.Lock)
                {
                    oldRoom.PlayerCount--;
                    if (oldRoom.PlayerCount <= 0) _rooms.TryRemove(oldRoomCode, out _);
                }
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, oldRoomCode);
            }

            _userRooms[Context.ConnectionId] = roomCode;
            lock (room.Lock)
            {
                room.PlayerCount++;
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

            // Send current state of the room to the new user
            lock (room.Lock)
            {
                if (room.StrokeHistory.Any())
                {
                    Clients.Caller.SendAsync("LoadHistory", room.StrokeHistory.ToArray());
                }
                
                if (!string.IsNullOrEmpty(room.CurrentBackground))
                {
                    Clients.Caller.SendAsync("ReceiveBackground", room.CurrentBackground);
                }

                if (room.IsGameRunning)
                {
                    Clients.Caller.SendAsync("GameStarted", new
                    {
                        drawerId = room.CurrentDrawerId,
                        drawerName = room.CurrentDrawerName,
                        endTime = room.GameEndTime,
                        isReconnect = true
                    });
                }
            }

            return true;
        }

        public async Task StartGame(string playerName)
        {
            if (!_userRooms.TryGetValue(Context.ConnectionId, out var roomCode) || !_rooms.TryGetValue(roomCode, out var room)) return;
            if (room.IsGameRunning) return;

            lock (room.Lock)
            {
                room.IsGameRunning = true;
                room.CurrentDrawerId = Context.ConnectionId;

                string name = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
                room.CurrentDrawerName = name.Length > 15 ? name.Substring(0, 15) : name;

                room.TargetWord = _words[_random.Next(_words.Length)];
                room.GameEndTime = DateTime.UtcNow.AddMinutes(1);

                room.StrokeHistory.Clear();
                room.CurrentBackground = "";

                if (room.GameTimer != null)
                {
                    room.GameTimer.Stop();
                    room.GameTimer.Dispose();
                }

                room.GameTimer = new System.Timers.Timer(1000);
                room.GameTimer.Elapsed += async (sender, e) =>
                {
                    if (DateTime.UtcNow >= room.GameEndTime)
                    {
                        await EndGame(roomCode);
                    }
                };
                room.GameTimer.Start();
            }

            await Clients.Group(roomCode).SendAsync("CanvasCleared");
            await Clients.Group(roomCode).SendAsync("GameStarted", new
            {
                drawerId = room.CurrentDrawerId,
                drawerName = room.CurrentDrawerName,
                endTime = room.GameEndTime
            });
            await Clients.Client(room.CurrentDrawerId).SendAsync("ReceiveWord", room.TargetWord);
        }

        public async Task MakeGuess(string guess, string playerName)
        {
            if (!_userRooms.TryGetValue(Context.ConnectionId, out var roomCode) || !_rooms.TryGetValue(roomCode, out var room)) return;
            if (!room.IsGameRunning || Context.ConnectionId == room.CurrentDrawerId) return;

            string safeGuess = (guess ?? "").Trim();
            if (safeGuess.Length > 50) safeGuess = safeGuess.Substring(0, 50);

            string safeName = (playerName ?? "Player");
            if (safeName.Length > 15) safeName = safeName.Substring(0, 15);

            bool isCorrect = string.Equals(safeGuess, room.TargetWord, StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                await EndGame(roomCode, safeName);
            }
            else
            {
                await Clients.Group(roomCode).SendAsync("ReceiveMessage", safeName, safeGuess, false);
            }
        }

        private async Task EndGame(string roomCode, string? winnerName = null)
        {
            if (!_rooms.TryGetValue(roomCode, out var room)) return;

            string word = room.TargetWord;
            room.IsGameRunning = false;
            
            if (room.GameTimer != null)
            {
                room.GameTimer.Stop();
                room.GameTimer.Dispose();
                room.GameTimer = null;
            }

            await _hubContext.Clients.Group(roomCode).SendAsync("GameEnded", new
            {
                winnerName = winnerName,
                word = word
            });
        }

        public async Task DrawLine(DrawData drawData)
        {
            if (!_userRooms.TryGetValue(Context.ConnectionId, out var roomCode) || !_rooms.TryGetValue(roomCode, out var room)) return;
            if (room.IsGameRunning && Context.ConnectionId != room.CurrentDrawerId) return;

            lock (room.Lock)
            {
                room.StrokeHistory.Add(drawData);
                if (room.StrokeHistory.Count > 50000) room.StrokeHistory.RemoveAt(0);
            }

            await Clients.GroupExcept(roomCode, Context.ConnectionId).SendAsync("ReceiveDraw", drawData);
        }

        public async Task UndoStroke()
        {
            if (!_userRooms.TryGetValue(Context.ConnectionId, out var roomCode) || !_rooms.TryGetValue(roomCode, out var room)) return;
            if (room.IsGameRunning && Context.ConnectionId != room.CurrentDrawerId) return;

            string? lastStrokeId = null;
            lock (room.Lock)
            {
                if (!room.StrokeHistory.Any()) return;

                lastStrokeId = room.StrokeHistory.Last().StrokeId;
                if (string.IsNullOrEmpty(lastStrokeId))
                {
                    room.StrokeHistory.RemoveAt(room.StrokeHistory.Count - 1);
                    return;
                }

                room.StrokeHistory.RemoveAll(d => d.StrokeId == lastStrokeId);
            }

            if (!string.IsNullOrEmpty(lastStrokeId))
            {
                await Clients.Group(roomCode).SendAsync("StrokeUndone", lastStrokeId);
            }
        }

        public async Task UpdateBackground(string base64Image)
        {
            if (!_userRooms.TryGetValue(Context.ConnectionId, out var roomCode) || !_rooms.TryGetValue(roomCode, out var room)) return;
            if (room.IsGameRunning) return;

            room.CurrentBackground = (base64Image?.Length < 5 * 1024 * 1024) ? base64Image : "";
            await Clients.GroupExcept(roomCode, Context.ConnectionId).SendAsync("ReceiveBackground", room.CurrentBackground);
        }

        public async Task ClearCanvas()
        {
            if (!_userRooms.TryGetValue(Context.ConnectionId, out var roomCode) || !_rooms.TryGetValue(roomCode, out var room)) return;
            if (room.IsGameRunning && Context.ConnectionId != room.CurrentDrawerId) return;

            lock (room.Lock)
            {
                room.StrokeHistory.Clear();
                room.CurrentBackground = "";
            }
            await Clients.Group(roomCode).SendAsync("CanvasCleared");
        }
    }
}
