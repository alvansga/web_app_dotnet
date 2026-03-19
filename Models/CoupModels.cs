using System;
using System.Collections.Generic;
using System.Linq;

namespace MyFirstApp.Models
{
    public class CoupPlayer
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty; 
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
        public List<string> PlayersPassedChallenge { get; set; } = new List<string>();
        public bool IsBlocked { get; set; }
        public string? BlockingPlayerId { get; set; }
        public string? BlockClaimedRole { get; set; }
        public List<string> PlayersPassedBlockChallenge { get; set; } = new List<string>();
    }

    public class CoupGame
    {
        public List<CoupPlayer> Players { get; set; } = new List<CoupPlayer>();
        public List<string> Deck { get; set; } = new List<string>();
        public int CurrentTurnIndex { get; set; } = 0;
        public string GameState { get; set; } = "Lobby"; // Lobby, Playing, WaitingForChallenge, WaitingForBlock, WaitingForBlockChallenge, GameOver
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
            foreach (var role in roles) { Deck.Add(role); Deck.Add(role); Deck.Add(role); }
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
            do { CurrentTurnIndex = (CurrentTurnIndex + 1) % Players.Count; } while (Players[CurrentTurnIndex].IsDead);
            Log += $" | Sekarang giliran: {Players[CurrentTurnIndex].Name}";
        }

        public void PerformAction(string actionType, string? targetId = null)
        {
            var player = Players[CurrentTurnIndex];
            if (actionType == "Income") { player.Coins += 1; Log = $"💰 {player.Name} Income (+1)."; NextTurn(); }
            else if (actionType == "Coup" && targetId != null)
            {
                if (player.Coins >= 7) { player.Coins -= 7; var target = Players.First(p => p.Id == targetId); Log = $"⚔️ {player.Name} COUP ke {target.Name}!"; ResolveInfluenceLoss(target); NextTurn(); }
            }
            else if (actionType == "ForeignAid") { PendingAction = new CoupPendingAction { ActionType = actionType, SourceId = player.Id }; PrepareChallenge(actionType, "All", ""); }
            else if (actionType == "Tax") { PrepareChallenge(actionType, "Duke", player.Id); }
            else if (actionType == "Assassinate" && targetId != null) 
            { 
               if (player.Coins >= 3) { player.Coins -= 3; PrepareChallenge(actionType, "Assassin", player.Id, targetId); }
            }
            else if (actionType == "Steal" && targetId != null) { PrepareChallenge(actionType, "Captain", player.Id, targetId); }
            else if (actionType == "Exchange") { PrepareChallenge(actionType, "Ambassador", player.Id); }
        }

        private void PrepareChallenge(string action, string role, string sourceId, string? targetId = null)
        {
            PendingAction = new CoupPendingAction { ActionType = action, ClaimedRole = role, SourceId = sourceId, TargetId = targetId };
            if (action == "ForeignAid") { Log = $"💸 {Players.First(p => p.Id == sourceId).Name} mengambil Foreign Aid (+2). Siapa yang mau BLOCK?"; TransitionToBlock(); }
            else { GameState = "WaitingForChallenge"; Log = $"📢 {Players.First(p => p.Id == sourceId).Name} melakukan {action} (Klaim {role}). Challenge?"; }
        }

        public void PassChallenge(string playerId)
        {
            if (PendingAction == null) return;
            if (GameState == "WaitingForChallenge")
            {
                if (!PendingAction.PlayersPassedChallenge.Contains(playerId)) PendingAction.PlayersPassedChallenge.Add(playerId);
                if (PendingAction.PlayersPassedChallenge.Count >= Players.Count(p => !p.IsDead) - 1) ResolveSuccessfulActionKlaim();
            }
            else if (GameState == "WaitingForBlockChallenge")
            {
                if (!PendingAction.PlayersPassedBlockChallenge.Contains(playerId)) PendingAction.PlayersPassedBlockChallenge.Add(playerId);
                if (PendingAction.PlayersPassedBlockChallenge.Count >= Players.Count(p => !p.IsDead) - 1) { Log = "⛔ Block berhasil!"; FinalizeAction(true); }
            }
        }

        public void Challenge(string challengerId)
        {
            if (PendingAction == null) return;
            var player = GameState == "WaitingForChallenge" ? Players.First(p => p.Id == PendingAction.SourceId) : Players.First(p => p.Id == PendingAction.BlockingPlayerId);
            var role = GameState == "WaitingForChallenge" ? PendingAction.ClaimedRole : PendingAction.BlockClaimedRole;
            var challenger = Players.First(p => p.Id == challengerId);

            Log = $"🔍 {challenger.Name} men-CHALLENGE {player.Name}!";
            if (player.Hand.Contains(role!))
            {
                Log += $" ✅ {player.Name} punya {role}! {challenger.Name} kehilangan pengaruh.";
                SwapCard(player, role!); ResolveInfluenceLoss(challenger);
                if (GameState == "WaitingForChallenge") ResolveSuccessfulActionKlaim(); else FinalizeAction(true);
            }
            else
            {
                Log += $" ❌ {player.Name} bohong! Tidak punya {role}.";
                ResolveInfluenceLoss(player);
                if (GameState == "WaitingForChallenge") { GameState = "Playing"; PendingAction = null; NextTurn(); } else { Log = "Aksi berlanjut karena Block gagal!"; FinalizeAction(false); }
            }
        }

        public void Block(string blockerId, string role)
        {
            if (GameState != "WaitingForBlock" || PendingAction == null) return;
            PendingAction.IsBlocked = true; PendingAction.BlockingPlayerId = blockerId; PendingAction.BlockClaimedRole = role;
            GameState = "WaitingForBlockChallenge"; Log = $"🛡️ {Players.First(p => p.Id == blockerId).Name} BLOCK {PendingAction.ActionType} (Klaim {role}). Challenge?";
        }

        public void PassBlock(string playerId) { if (GameState == "WaitingForBlock" && PendingAction != null && playerId == PendingAction.TargetId) FinalizeAction(false); }

        private void TransitionToBlock()
        {
            if (PendingAction == null) return;
            if (PendingAction.ActionType == "ForeignAid" || PendingAction.ActionType == "Assassinate" || PendingAction.ActionType == "Steal") GameState = "WaitingForBlock";
            else ResolveActionEffect();
        }

        private void ResolveSuccessfulActionKlaim() { TransitionToBlock(); }

        private void FinalizeAction(bool blocked)
        {
            if (blocked) { GameState = "Playing"; PendingAction = null; NextTurn(); }
            else ResolveActionEffect();
        }

        private void ResolveActionEffect()
        {
            if (PendingAction == null) return;
            var player = Players.First(p => p.Id == PendingAction.SourceId);
            if (PendingAction.ActionType == "Tax") { player.Coins += 3; Log = $"💰 {player.Name} Tax (+3)."; }
            else if (PendingAction.ActionType == "ForeignAid") { player.Coins += 2; Log = $"💸 {player.Name} Foreign Aid (+2)."; }
            else if (PendingAction.ActionType == "Steal" && PendingAction.TargetId != null)
            {
                var target = Players.First(p => p.Id == PendingAction.TargetId);
                int amount = Math.Min(target.Coins, 2); target.Coins -= amount; player.Coins += amount; Log = $"💰 {player.Name} mencuri {amount} koin dari {target.Name}.";
            }
            else if (PendingAction.ActionType == "Assassinate" && PendingAction.TargetId != null)
            {
                var target = Players.First(p => p.Id == PendingAction.TargetId);
                Log = $"💀 {player.Name} meng-assassinate {target.Name}!"; ResolveInfluenceLoss(target);
            }
            else if (PendingAction.ActionType == "Exchange")
            {
                // Sederhananya, Ambassador ambil 2 kartu baru, kocok balik 2 kartu lama/baru
                Log = $"🃏 {player.Name} Ambassador Exchange (Kartu diganti baru).";
                SwapCard(player, player.Hand[0]); if (player.Hand.Count > 1) SwapCard(player, player.Hand[1]);
            }
            GameState = "Playing"; PendingAction = null; NextTurn();
        }

        private void SwapCard(CoupPlayer p, string role) { p.Hand.Remove(role); Deck.Add(role); Random rnd = new Random(); Deck = Deck.OrderBy(x => rnd.Next()).ToList(); p.Hand.Add(Deck[0]); Deck.RemoveAt(0); }
        private void ResolveInfluenceLoss(CoupPlayer target) { if (target.Hand.Count > 0) { string lost = target.Hand[0]; target.Hand.RemoveAt(0); target.Revealed.Add(lost); Log += $" {target.Name} kehilangan {lost}."; } }
    }
}
