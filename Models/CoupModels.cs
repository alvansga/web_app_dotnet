using System;
using System.Collections.Generic;
using System.Linq;

namespace MyFirstApp.Models
{
    public class CoupPlayer
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty; // metadata seperti "Pemain Agresif" dsb.
        public int Coins { get; set; } = 2;
        public List<string> Hand { get; set; } = new List<string>();
        public List<string> Revealed { get; set; } = new List<string>();
        public bool IsDead => Hand.Count == 0;
    }

    public class CoupGame
    {
        public List<CoupPlayer> Players { get; set; } = new List<CoupPlayer>();
        public List<string> Deck { get; set; } = new List<string>();
        public int CurrentTurnIndex { get; set; } = 0;
        public string GameState { get; set; } = "Lobby"; // Lobby, Playing, GameOver
        public string Log { get; set; } = "Menunggu pemain masuk...";

        public void AddPlayer(string name, string description)
        {
            Players.Add(new CoupPlayer { Name = name, Description = description });
        }

        public void StartGame()
        {
            if (Players.Count < 2) return;
            
            Deck.Clear();
            string[] roles = { "Duke", "Assassin", "Captain", "Ambassador", "Contessa" };
            foreach (var role in roles)
            {
                Deck.Add(role); Deck.Add(role); Deck.Add(role);
            }
            
            Random rnd = new Random();
            Deck = Deck.OrderBy(x => rnd.Next()).ToList();

            foreach (var player in Players)
            {
                player.Coins = 2;
                player.Hand.Clear();
                player.Revealed.Clear();
                player.Hand.Add(Deck[0]); Deck.RemoveAt(0);
                player.Hand.Add(Deck[0]); Deck.RemoveAt(0);
            }
            
            GameState = "Playing";
            CurrentTurnIndex = 0;
            Log = "Game dimulai! Giliran " + Players[0].Name;
        }

        public void NextTurn()
        {
            if (Players.Count(p => !p.IsDead) <= 1)
            {
                GameState = "GameOver";
                Log = $"🏆 {Players.First(p => !p.IsDead).Name} MEMENANGKAN GAME!";
                return;
            }

            do
            {
                CurrentTurnIndex = (CurrentTurnIndex + 1) % Players.Count;
            } while (Players[CurrentTurnIndex].IsDead);
            
            Log += $" | Sekarang giliran: {Players[CurrentTurnIndex].Name}";
        }

        public void PerformAction(string actionType, string targetId = null)
        {
            var player = Players[CurrentTurnIndex];
            if (actionType == "Income")
            {
                player.Coins += 1;
                Log = $"💰 {player.Name} mengambil Income (+1 Koin).";
                NextTurn();
            }
            else if (actionType == "ForeignAid")
            {
                player.Coins += 2;
                Log = $"💸 {player.Name} mengambil Foreign Aid (+2 Koin).";
                NextTurn();
            }
            else if (actionType == "Tax")
            {
                player.Coins += 3;
                Log = $"👑 {player.Name} (Duke) menarik Tax (+3 Koin).";
                NextTurn();
            }
            else if (actionType == "Coup" && targetId != null)
            {
                if (player.Coins >= 7)
                {
                    player.Coins -= 7;
                    var target = Players.First(p => p.Id == targetId);
                    Log = $"⚔️ {player.Name} melancarkan COUP kepada {target.Name}!";
                    if (target.Hand.Count > 0)
                    {
                        string killed = target.Hand[0];
                        target.Hand.RemoveAt(0);
                        target.Revealed.Add(killed);
                        Log += $" {target.Name} kehilangan {killed}.";
                    }
                    NextTurn();
                }
            }
        }
    }
}
