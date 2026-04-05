using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System;
using System.Timers;
using WebAppSandbox.Models;

namespace WebAppSandbox.Hubs
{
    public class DrawingHub : Hub
    {
        private static readonly List<DrawData> _strokeHistory = new();
        private static readonly object _lock = new();
        private static readonly GameState _gameState = new();

        private readonly IHubContext<DrawingHub> _hubContext;

        public DrawingHub(IHubContext<DrawingHub> hubContext)
        {
            _hubContext = hubContext;
        }
        private static readonly string[] _words = {
            // MARVEL & DC
            "IRON MAN", "SPIDER-MAN", "THOR", "HULK", "BLACK WIDOW", "CAPTAIN AMERICA", "GROOT", "THANOS", "LOKI", "WOLVERINE",
            "BATMAN", "SUPERMAN", "WONDER WOMAN", "THE FLASH", "AQUAMAN", "JOKER", "HARLEY QUINN", "BLACK PANTHER", "DOCTOR STRANGE", "VENOM",

            // DISNEY & PIXAR
            "MICKEY MOUSE", "DONALD DUCK", "GOOFY", "ELSA", "ANNA", "OLAF", "SIMBA", "ALADDIN", "GENIE", "ARIEL",
            "MULAN", "STITCH", "BAYMAX", "WINNIE THE POOH", "MALEFICENT", "PETER PAN", "HERCULES", "MOANA", "MAUI", "RAPUNZEL",
            "WOODY", "BUZZ LIGHTYEAR", "NEMO", "DORY", "WALL-E", "EVE", "REMY", "SULLY", "MIKE WAZOWSKI", "LIGHTNING MCQUEEN", "TOW MATER",
            "MR. INCREDIBLE", "ELASTIGIRL", "JOY", "SADNESS", "BING BONG", "RUSSELL", "CARL FREDRICKSEN",

            // ANIME & MANGA
            "NARUTO", "SASUKE", "KAKASHI", "ITACHI", "GAARA", "KURAMA", "HINATA", "MADARA", "TSUNADE", "JIRAIYA",
            "LUFFY", "ZORO", "NAMI", "SANJI", "CHOPPER", "ROBIN", "BROOK", "SHANKS", "ACE", "KAIDO",
            "GOKU", "VEGETA", "FRIEZA", "CELL", "MAJIN BUU", "PIKACHU", "CHARIZARD", "DORAEMON", "TOTORO", "SAITAMA",
            "TANJIRO", "NEZUKO", "ZENITSU", "INOSUKE", "MUZAN", "RENGOKU", "LIGHT YAGAMI", "RYUK", "EDWARD ELRIC", "ALPHONSE ELRIC",

            // MY HERO ACADEMIA
            "DEKU", "ALL MIGHT", "BAKUGO", "TODOROKI", "URARAKA", "IIDA", "FROPPY", "KIRISHIMA", "ENDEAVOR", "ERASERHEAD",
            "SHIGARAKI", "TOGA", "DABI", "ALL FOR ONE", "MIRIO",

            // ATTACK ON TITAN
            "EREN YEAGER", "MIKASA ACKERMAN", "LEVI ACKERMAN", "ARMIN ARLERT", "ERWIN SMITH", "REINER BRAUN", "BERTHOLDT", "ZEKE YEAGER",
            "COLOSSAL TITAN", "ARMORED TITAN",

            // NICKELODEON
            "SPONGEBOB", "PATRICK STAR", "SQUIDWARD", "MR. KRABS", "SANDY CHEEKS", "PLANKTON", "GARY THE SNAIL",
            "AANG", "KATARA", "SOKKA", "ZUKO", "TOPH", "APPA", "MOMO", "KORRA",
            "DANNY PHANTOM", "TIMMY TURNER", "COSMO", "WANDA", "JIMMY NEUTRON", "ARNOLD SHORTMAN", "CATDOG",

            // CARTOON NETWORK
            "BEN 10", "GWEN TENNYSON", "KEVIN LEVIN", "BLOSSOM", "BUBBLES", "BUTTERCUP",
        };
        private static readonly Random _random = new();

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

            if (!string.IsNullOrEmpty(_gameState.CurrentBackground))
            {
                await Clients.Caller.SendAsync("ReceiveBackground", _gameState.CurrentBackground);
            }

            // Sync game state for new client
            if (_gameState.IsGameRunning)
            {
                await Clients.Caller.SendAsync("GameStarted", new
                {
                    drawerId = _gameState.CurrentDrawerId,
                    drawerName = _gameState.CurrentDrawerName,
                    endTime = _gameState.GameEndTime,
                    isReconnect = true
                });
            }
        }

        public async Task StartGame(string playerName)
        {
            if (_gameState.IsGameRunning) return;

            lock (_lock)
            {
                _gameState.IsGameRunning = true;
                _gameState.CurrentDrawerId = Context.ConnectionId;

                // Security: Limit name length
                string name = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
                _gameState.CurrentDrawerName = name.Length > 15 ? name.Substring(0, 15) : name;

                _gameState.TargetWord = _words[_random.Next(_words.Length)];
                _gameState.GameEndTime = DateTime.UtcNow.AddMinutes(1);

                // Reset canvas
                _strokeHistory.Clear();
                _gameState.CurrentBackground = "";

                if (_gameState.GameTimer != null)
                {
                    _gameState.GameTimer.Stop();
                    _gameState.GameTimer.Dispose();
                }

                _gameState.GameTimer = new System.Timers.Timer(1000);
                _gameState.GameTimer.Elapsed += async (sender, e) =>
                {
                    if (DateTime.UtcNow >= _gameState.GameEndTime)
                    {
                        await EndGame(null, _gameState.TargetWord);
                    }
                };
                _gameState.GameTimer.Start();
            }

            await Clients.All.SendAsync("CanvasCleared");
            await Clients.All.SendAsync("GameStarted", new
            {
                drawerId = _gameState.CurrentDrawerId,
                drawerName = _gameState.CurrentDrawerName,
                endTime = _gameState.GameEndTime
            });
            await Clients.Caller.SendAsync("ReceiveWord", _gameState.TargetWord);
        }

        public async Task MakeGuess(string guess, string playerName)
        {
            if (!_gameState.IsGameRunning || Context.ConnectionId == _gameState.CurrentDrawerId) return;

            // Security: Sanitize inputs
            string safeGuess = string.IsNullOrEmpty(guess) ? "" : guess.Trim();
            if (safeGuess.Length > 50) safeGuess = safeGuess.Substring(0, 50);

            string safeName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
            if (safeName.Length > 15) safeName = safeName.Substring(0, 15);

            bool isCorrect = string.Equals(safeGuess, _gameState.TargetWord, StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                await EndGame(safeName, _gameState.TargetWord);
            }
            else
            {
                await Clients.All.SendAsync("ReceiveMessage", safeName, safeGuess, false);
            }
        }

        private async Task EndGame(string? winnerName, string word)
        {
            _gameState.IsGameRunning = false;
            if (_gameState.GameTimer != null)
            {
                _gameState.GameTimer.Stop();
                _gameState.GameTimer.Dispose();
                _gameState.GameTimer = null;
            }

            // Use _hubContext instead of Clients because this may be called from a background timer
            // after the original Hub instance has been disposed.
            await _hubContext.Clients.All.SendAsync("GameEnded", new
            {
                winnerName = winnerName,
                word = word
            });
        }

        public async Task DrawLine(DrawData drawData)
        {
            // If game is running, only the current drawer can draw
            if (_gameState.IsGameRunning && Context.ConnectionId != _gameState.CurrentDrawerId)
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
            if (_gameState.IsGameRunning && Context.ConnectionId != _gameState.CurrentDrawerId) return;

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
            if (_gameState.IsGameRunning) return; // Background not allowed during game

            // Security: Limit image size to ~5MB
            if (string.IsNullOrEmpty(base64Image) || base64Image.Length > 5 * 1024 * 1024)
            {
                _gameState.CurrentBackground = "";
            }
            else
            {
                _gameState.CurrentBackground = base64Image;
            }

            await Clients.Others.SendAsync("ReceiveBackground", _gameState.CurrentBackground);
        }

        public async Task ClearCanvas()
        {
            if (_gameState.IsGameRunning && Context.ConnectionId != _gameState.CurrentDrawerId) return;

            lock (_lock)
            {
                _strokeHistory.Clear();
                _gameState.CurrentBackground = "";
            }
            await Clients.All.SendAsync("CanvasCleared");
        }
    }
}
