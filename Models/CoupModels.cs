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

    public class CoupPendingAction
    {
        public string ActionType { get; set; } = "";
        public string SourceId { get; set; } = "";
        public string? TargetId { get; set; }
        public string ClaimedRole { get; set; } = "";
        public List<string> PlayersPassed { get; set; } = new List<string>(); // Siapa saja yang sudah bilang OK / Gak challenge
    }

    public class CoupGame
    {
        public List<CoupPlayer> Players { get; set; } = new List<CoupPlayer>();
        public List<string> Deck { get; set; } = new List<string>();
        public int CurrentTurnIndex { get; set; } = 0;
        public string GameState { get; set; } = "Lobby"; // Lobby, Playing, WaitingForChallenge, GameOver
        public string Log { get; set; } = "Menunggu pemain masuk...";
        public CoupPendingAction? PendingAction { get; set; }

        public CoupPlayer AddPlayer(string name, string description)
        {
            var p = new CoupPlayer { Name = name, Description = description };
            Players.Add(p);
            return p;
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

        public void PerformAction(string actionType, string? targetId = null)
        {
            var player = Players[CurrentTurnIndex];

            // Aksi yang TIDAK bisa dichallenge
            if (actionType == "Income")
            {
                player.Coins += 1;
                Log = $"💰 {player.Name} mengambil Income (+1 Koin).";
                NextTurn();
            }
            else if (actionType == "ForeignAid")
            {
                // Note: Foreign Aid bisa diBlock, tapi as requested kita fokus Challenge dulu.
                player.Coins += 2;
                Log = $"💸 {player.Name} mengambil Foreign Aid (+2 Koin).";
                NextTurn();
            }
            else if (actionType == "Tax")
            {
                // Challengable (Requires Duke)
                PendingAction = new CoupPendingAction { 
                    ActionType = actionType, 
                    SourceId = player.Id, 
                    ClaimedRole = "Duke" 
                };
                GameState = "WaitingForChallenge";
                Log = $"👑 {player.Name} mengklaim DUKE dan menarik TAX (+3 Koin). Siapa yang mau Challenge?";
            }
            else if (actionType == "Coup" && targetId != null)
            {
                if (player.Coins >= 7)
                {
                    player.Coins -= 7;
                    var target = Players.First(p => p.Id == targetId);
                    Log = $"⚔️ {player.Name} melancarkan COUP kepada {target.Name}!";
                    ResolveInfluenceLoss(target);
                    NextTurn();
                }
            }
        }

        public void PassChallenge(string playerId)
        {
            if (GameState != "WaitingForChallenge" || PendingAction == null) return;
            
            if (!PendingAction.PlayersPassed.Contains(playerId))
                PendingAction.PlayersPassed.Add(playerId);

            // Jika semua (selain aktor) sudah pass, lakukan aksi
            int aliveCount = Players.Count(p => !p.IsDead);
            if (PendingAction.PlayersPassed.Count >= aliveCount - 1)
            {
                ResolvePendingAction();
            }
        }

        public void Challenge(string challengerId)
        {
            if (GameState != "WaitingForChallenge" || PendingAction == null) return;

            var player = Players.First(p => p.Id == PendingAction.SourceId);
            var challenger = Players.First(p => p.Id == challengerId);

            Log = $"🔍 {challenger.Name} men-CHALLENGE klaim {player.Name}!";

            if (player.Hand.Contains(PendingAction.ClaimedRole))
            {
                // Player punya kartu tersebut -> Challenger kalah
                Log += $" ✅ {player.Name} TERNYATA PUNYA {PendingAction.ClaimedRole}! {challenger.Name} kehilangan 1 pengaruh.";
                
                // Player kocok ulang kartu yang benar
                SwapCard(player, PendingAction.ClaimedRole);
                
                ResolveInfluenceLoss(challenger);
                ResolvePendingAction(); // Aksi tetap jalan
            }
            else
            {
                // Player bohong -> Player kalah
                Log += $" ❌ {player.Name} TERNYATA BERBOHONG! Tidak punya {PendingAction.ClaimedRole}.";
                ResolveInfluenceLoss(player);
                
                // Aksi batal
                GameState = "Playing";
                PendingAction = null;
                NextTurn();
            }
        }

        private void SwapCard(CoupPlayer p, string role)
        {
            p.Hand.Remove(role);
            Deck.Add(role);
            Random rnd = new Random();
            Deck = Deck.OrderBy(x => rnd.Next()).ToList();
            p.Hand.Add(Deck[0]); Deck.RemoveAt(0);
        }

        private void ResolveInfluenceLoss(CoupPlayer target)
        {
            if (target.Hand.Count > 0)
            {
                string lost = target.Hand[0];
                target.Hand.RemoveAt(0);
                target.Revealed.Add(lost);
                Log += $" {target.Name} kehilangan {lost}.";
            }
        }

        private void ResolvePendingAction()
        {
            if (PendingAction == null) return;
            var player = Players.First(p => p.Id == PendingAction.SourceId);

            if (PendingAction.ActionType == "Tax")
            {
                player.Coins += 3;
                Log = $"💰 {player.Name} berhasil menarik Tax (+3 Koin).";
            }

            GameState = "Playing";
            PendingAction = null;
            NextTurn();
        }
    }
}
