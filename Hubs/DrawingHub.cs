using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System;
using System.Timers;

namespace WebAppSandbox.Hubs
{
    public class DrawingHub : Hub
    {
        private static readonly List<DrawData> _strokeHistory = new();
        private static readonly object _lock = new();
        private static string _currentBackground = "";

        private readonly IHubContext<DrawingHub> _hubContext;

        public DrawingHub(IHubContext<DrawingHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // Game State
        private static bool _isGameRunning = false;
        private static string _currentDrawerId = "";
        private static string _currentDrawerName = "";
        private static string _targetWord = "";
        private static DateTime _gameEndTime;
        private static System.Timers.Timer _gameTimer;
        private static readonly string[] _words = { 
            "DOG", "CAT", "LION", "ELEPHANT", "SHARK", "OWL", "BEE", "TURTLE", "DRAGON", "PENGUIN", 
            "GIRAFFE", "KANGAROO", "MONKEY", "PIG", "RABBIT", "SNAKE", "WHALE", "SPIDER", "HORSE", "ZEBRA",
            "PIZZA", "BURGER", "APPLE", "BANANA", "ICE CREAM", "CAKE", "SUSHI", "TACO", "DONUT", "COOKIE",
            "CARROT", "CORN", "BROCCOLI", "WATERMELON", "PINEAPPLE", "CUPCAKE", "CHEESE", "EGG", "LEMON", "STRAWBERRY",
            "CHAIR", "TABLE", "LAMP", "BED", "FAN", "CLOCK", "PHONE", "COMPUTER", "CAMERA", "GUITAR",
            "UMBRELLA", "KEYS", "BOOKS", "SCISSORS", "MIRROR", "WALLET", "BOTTLE", "SPOON", "FORK", "KNIFE",
            "CAR", "BUS", "TRAIN", "AIRPLANE", "HELICOPTER", "BICYCLE", "BOAT", "ROCKET", "TRUCK", "SUBMARINE",
            "TREE", "FLOWER", "SUN", "MOON", "CLOUD", "STAR", "RAIN", "MOUNTAIN", "VOLCANO", "ISLAND",
            "FIRE", "SNOWMAN", "RAINBOW", "LEAF", "MUSHROOM", "HOUSE", "SCHOOL", "BRIDGE", "FENCE", "HAMMER",
            "SCREWDRIVER", "PENCIL", "BALLOON", "HEART", "DIAMOND", "CROWN", "SWORD", "SHIELD", "MAP", "FLAG",
            "HAT", "SHIRT", "PANTS", "SHOES", "SOCKS", "GLASSES", "DRESS", "JACKET", "SCARF", "TIE",
            "BREAD", "BACON", "SOUP", "COFFEE", "MILK", "JUICE", "COOKIE", "POPCORN", "GRAPES", "CHERRY"
        };
        private static readonly Random _random = new();

        public class DrawData
        {
            [JsonPropertyName("lastX")]
            public double LastX { get; set; }

            [JsonPropertyName("lastY")]
            public double LastY { get; set; }

            [JsonPropertyName("x")]
            public double X { get; set; }

            [JsonPropertyName("y")]
            public double Y { get; set; }

            [JsonPropertyName("color")]
            public string Color { get; set; }

            [JsonPropertyName("size")]
            public int Size { get; set; }

            [JsonPropertyName("isEraser")]
            public bool IsEraser { get; set; }
            
            [JsonPropertyName("strokeId")]
            public string StrokeId { get; set; }
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            lock (_lock)
            {
                if (_strokeHistory.Any())
                {
                    Clients.Caller.SendAsync("LoadHistory", _strokeHistory.ToArray());
                }
            }

            if (!string.IsNullOrEmpty(_currentBackground))
            {
                await Clients.Caller.SendAsync("ReceiveBackground", _currentBackground);
            }

            // Sync game state for new client
            if (_isGameRunning)
            {
                await Clients.Caller.SendAsync("GameStarted", new { 
                    drawerId = _currentDrawerId, 
                    drawerName = _currentDrawerName,
                    endTime = _gameEndTime,
                    isReconnect = true
                });
            }
        }

        public async Task StartGame(string playerName)
        {
            if (_isGameRunning) return;

            lock (_lock)
            {
                _isGameRunning = true;
                _currentDrawerId = Context.ConnectionId;
                _currentDrawerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
                _targetWord = _words[_random.Next(_words.Length)];
                _gameEndTime = DateTime.UtcNow.AddMinutes(1);
                
                // Reset canvas
                _strokeHistory.Clear();
                _currentBackground = "";

                if (_gameTimer != null)
                {
                    _gameTimer.Stop();
                    _gameTimer.Dispose();
                }

                _gameTimer = new System.Timers.Timer(1000);
                _gameTimer.Elapsed += async (sender, e) => 
                {
                    if (DateTime.UtcNow >= _gameEndTime)
                    {
                        await EndGame(null, _targetWord);
                    }
                };
                _gameTimer.Start();
            }

            await Clients.All.SendAsync("CanvasCleared");
            await Clients.All.SendAsync("GameStarted", new { 
                drawerId = _currentDrawerId, 
                drawerName = _currentDrawerName,
                endTime = _gameEndTime
            });
            await Clients.Caller.SendAsync("ReceiveWord", _targetWord);
        }

        public async Task MakeGuess(string guess, string playerName)
        {
            if (!_isGameRunning || Context.ConnectionId == _currentDrawerId) return;

            bool isCorrect = string.Equals(guess.Trim(), _targetWord, StringComparison.OrdinalIgnoreCase);
            
            if (isCorrect)
            {
                await EndGame(playerName, _targetWord);
            }
            else
            {
                await Clients.All.SendAsync("ReceiveMessage", playerName, guess, false);
            }
        }

        private async Task EndGame(string? winnerName, string word)
        {
            _isGameRunning = false;
            if (_gameTimer != null)
            {
                _gameTimer.Stop();
                _gameTimer.Dispose();
                _gameTimer = null;
            }

            // Use _hubContext instead of Clients because this may be called from a background timer
            // after the original Hub instance has been disposed.
            await _hubContext.Clients.All.SendAsync("GameEnded", new {
                winnerName = winnerName,
                word = word
            });
        }

        public async Task DrawLine(DrawData drawData)
        {
            // If game is running, only the current drawer can draw
            if (_isGameRunning && Context.ConnectionId != _currentDrawerId)
            {
                return;
            }

            lock (_lock)
            {
                _strokeHistory.Add(drawData);
                if (_strokeHistory.Count > 50000)
                {
                    _strokeHistory.RemoveAt(0);
                }
            }

            await Clients.Others.SendAsync("ReceiveDraw", drawData);
        }

        public async Task UndoStroke()
        {
            if (_isGameRunning && Context.ConnectionId != _currentDrawerId) return;

            string lastStrokeId = null;
            int removedCount = 0;

            lock (_lock)
            {
                if (!_strokeHistory.Any()) return;
                
                lastStrokeId = _strokeHistory.Last().StrokeId;
                if (string.IsNullOrEmpty(lastStrokeId))
                {
                    _strokeHistory.RemoveAt(_strokeHistory.Count - 1);
                    return;
                }

                removedCount = _strokeHistory.RemoveAll(d => d.StrokeId == lastStrokeId);
            }

            if (!string.IsNullOrEmpty(lastStrokeId))
            {
                await Clients.All.SendAsync("StrokeUndone", lastStrokeId);
            }
        }

        public async Task UpdateBackground(string base64Image)
        {
            if (_isGameRunning) return; // Background not allowed during game

            _currentBackground = base64Image;
            await Clients.Others.SendAsync("ReceiveBackground", base64Image);
        }

        public async Task ClearCanvas()
        {
            if (_isGameRunning && Context.ConnectionId != _currentDrawerId) return;

            lock (_lock)
            {
                _strokeHistory.Clear();
                _currentBackground = "";
            }
            await Clients.All.SendAsync("CanvasCleared");
        }
    }
}
