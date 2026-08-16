using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.GameEngine.Rules;

namespace WebAppSandbox.Cli;

internal static class Program
{
    private const string AliceId = "alice";
    private const string BobId = "bob";

    private static OrganAttackGame _engine = null!;
    private static Game _game = null!;
    private static bool _exitRequested;

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== Organ Attack CLI ===");
        Console.WriteLine("Test API/GameEngine tanpa frontend. Pemain: Alice vs Bob.\n");

        var game = new Game("cli-room");
        _engine = new OrganAttackGame(game);
        _game = game;

        _engine.AddPlayer(AliceId, "Alice");
        _engine.AddPlayer(BobId, "Bob");
        _engine.StartGame(new Random());

        RunGameLoop();
    }

    private static void RunGameLoop()
    {
        while (!_exitRequested && _game.Phase == GamePhase.Playing)
        {
            var current = _game.CurrentPlayer!;
            RenderBoard();

            Console.WriteLine($"--- Giliran {current.Name} ---");
            RenderHand(current);

            var command = Prompt(player: current);

            if (command is null)
            {
                // EOF (mis. input dipipe dari stdin) -> keluar bersih
                _exitRequested = true;
                break;
            }

            switch (command)
            {
                case "play":
                case "p":
                    HandlePlay(current);
                    break;
                case "draw":
                case "d":
                    Safe(() => _engine.DrawCard(current.Id));
                    break;
                case "end":
                case "e":
                    Safe(() => _engine.EndTurn(current.Id));
                    break;
                case "help":
                case "h":
                    PrintHelp();
                    break;
                case "quit":
                case "q":
                    _exitRequested = true;
                    break;
                default:
                    Console.WriteLine("Perintah tidak dikenal. Ketik 'help' untuk daftar.");
                    break;
            }
        }

        if (_game.Phase == GamePhase.GameOver)
        {
            RenderBoard();
            var winner = _game.Players.FirstOrDefault(p => p.Id == _game.WinnerPlayerId);
            Console.WriteLine($"\n=== GAME OVER ===");
            Console.WriteLine($"Pemenang: {winner?.Name ?? "?"}");
        }
    }

    private static string? Prompt(Player player)
    {
        Console.Write($"[{player.Name}] aksi (play/draw/end/help/quit): ");
        var input = Console.ReadLine()?.Trim().ToLowerInvariant();
        Console.WriteLine();
        return input;
    }

    private static void HandlePlay(Player player)
    {
        var hand = player.Hand;
        if (hand.Count == 0)
        {
            Console.WriteLine("Tangan kosong. Ketik 'draw' untuk ambil kartu.");
            return;
        }

        // Pilih kartu
        Console.WriteLine("Pilih kartu:");
        for (int i = 0; i < hand.Count; i++)
        {
            Console.WriteLine($"  [{i + 1}] {DescribeCard(hand[i])}");
        }

        Console.Write("Nomor kartu: ");
        if (!int.TryParse(Console.ReadLine(), out var cardIndex) || cardIndex < 1 || cardIndex > hand.Count)
        {
            Console.WriteLine("Nomor kartu tidak valid.");
            return;
        }

        var card = hand[cardIndex - 1];

        // Tentukan pemilik organ target
        Player targetOwner;
        if (card.TargetSide == TargetSide.Opponent)
        {
            targetOwner = _game.OpponentOf(player.Id)!;
            Console.WriteLine($"Kartu menargetkan lawan: {targetOwner.Name}");
        }
        else if (card.TargetSide == TargetSide.Self)
        {
            targetOwner = player;
            Console.WriteLine($"Kartu menargetkan diri sendiri: {player.Name}");
        }
        else
        {
            Console.WriteLine("Kartu ini tidak punya target valid.");
            return;
        }

        // Pilih organ target
        Console.WriteLine("Pilih organ target:");
        var aliveOrgans = targetOwner.Organs
            .Select((organ, index) => (organ, index))
            .ToList();

        for (int i = 0; i < aliveOrgans.Count; i++)
        {
            var (organ, _) = aliveOrgans[i];
            Console.WriteLine($"  [{i + 1}] {DescribeOrgan(organ)}");
        }

        Console.Write("Nomor organ: ");
        if (!int.TryParse(Console.ReadLine(), out var organIndex) || organIndex < 1 || organIndex > aliveOrgans.Count)
        {
            Console.WriteLine("Nomor organ tidak valid.");
            return;
        }

        var targetOrgan = aliveOrgans[organIndex - 1].organ;

        Console.WriteLine("\nMelakukan aksi:");
        Console.WriteLine($"  {player.Name} memainkan '{card.Name}' -> {targetOwner.Name}'s {targetOrgan.Type}");

        Safe(() => _engine.PlayCard(player.Id, card.Id, targetOwner.Id, targetOrgan.Type));
    }

    private static void ShowHand(Player player)
    {
        if (player.Hand.Count == 0)
        {
            Console.WriteLine("  (kosong)");
            return;
        }

        for (int i = 0; i < player.Hand.Count; i++)
        {
            Console.WriteLine($"  [{i + 1}] {DescribeCard(player.Hand[i])}");
        }
    }

    private static void RenderHand(Player player)
    {
        Console.WriteLine($"Tangan {player.Name} ({player.Hand.Count} kartu):");
        ShowHand(player);
        Console.WriteLine();
    }

    private static void RenderBoard()
    {
        Console.WriteLine(new string('=', 60));

        foreach (var player in _game.Players)
        {
            Console.WriteLine($"Player: {player.Name} {(player.Id == _game.Turn?.CurrentPlayerId ? "<-- giliran" : "")}");
            for (int i = 0; i < player.Organs.Count; i++)
            {
                Console.WriteLine($"  [{i + 1}] {DescribeOrgan(player.Organs[i])}");
            }

            if (player.IsAlive == false)
            {
                Console.WriteLine("  *** SEMUA ORGAN HANCUR ***");
            }

            Console.WriteLine($"  Tangan: {player.Hand.Count} kartu");
            Console.WriteLine();
        }

        Console.WriteLine($"Deck: {_game.Deck?.DrawPile.Count ?? 0} kartu | Discard: {_game.Deck?.DiscardPile.Count ?? 0} kartu");
        Console.WriteLine($"Phase: {_game.Phase}");
        Console.WriteLine(new string('=', 60));
    }

    private static string DescribeCard(Card card)
    {
        if (card.RequiresOrganMatch())
        {
            return $"{card.Name} ({card.Type} -> {card.TargetOrganType})";
        }

        return $"{card.Name} ({card.Type})";
    }

    private static string DescribeOrgan(Organ organ)
    {
        var flags = new List<string>();
        if (organ.IsDestroyed)
        {
            flags.Add("DESTROYED");
        }

        if (organ.IsShielded)
        {
            flags.Add("SHIELD");
        }

        if (organ.IsAfflicted)
        {
            flags.Add($"AFFLICTED x{organ.Afflictions.Count}");
        }

        var suffix = flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : "";
        return $"{organ.Type}{suffix}";
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Perintah tersedia:");
        Console.WriteLine("  play  (p)  - mainkan kartu dari tangan");
        Console.WriteLine("  draw  (d)  - ambil satu kartu dari deck");
        Console.WriteLine("  end   (e)  - akhiri giliran");
        Console.WriteLine("  help  (h)  - tampilkan bantuan ini");
        Console.WriteLine("  quit  (q)  - keluar dari permainan");
        Console.WriteLine();
    }

    private static void Safe(Action action)
    {
        try
        {
            action();
        }
        catch (GameRuleException ex)
        {
            Console.WriteLine($"! Aksi ditolak: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"! Error: {ex.Message}");
        }

        Console.WriteLine();
    }
}