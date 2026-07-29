using System.IO;
using System.Linq;
using CodenameApp.Application.Interfaces;

public class StartGameService
{
    private readonly IGameRoomRepository _roomRepo;
    private readonly ICodenameRepository _codenameRepo;
    private readonly IPlayerRepository _playerRepo;

    public StartGameService(IGameRoomRepository roomRepo, ICodenameRepository codenameRepo, IPlayerRepository playerRepo)
    {
        _roomRepo = roomRepo;
        _codenameRepo = codenameRepo;
        _playerRepo = playerRepo;
    }

    public async Task Execute(string code, Guid starterId, string token)
    {
        // Validate player credentials
        var player = await _playerRepo.GetByIdAndTokenAsync(starterId, token)
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        var room = await _roomRepo.GetByCodeAsync(code);
        if (room == null) throw new Exception("Room not found");

        // Load words from words.txt
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "words-id.txt");
        // Fallback to project root if BaseDirectory is bin/Debug/...
        if (!File.Exists(filePath))
        {
            filePath = "words-id.txt";
        }

        List<string> words;
        if (File.Exists(filePath))
        {
            words = File.ReadAllLines(filePath)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .Select(line => line.Trim().ToUpper())
                        .ToList();
        }
        else
        {
            // Emergency fallback if file is totally missing
            words = new List<string> {
                "APPLE", "BANANA", "CHERRY", "DOG", "ELEPHANT", "FOX", "GRAPE", "HORSE", "IGLOO", "JUMP",
                "KITE", "LION", "MOUSE", "NIGHT", "OWL", "PIG", "QUEEN", "RABBIT", "SNAKE", "TIGER",
                "UMBRELLA", "VAN", "WHALE", "XYLOPHONE", "YACHT", "ZEBRA"
            };
        }

        var random = new Random();
        var selectedWords = words.OrderBy(x => random.Next()).Take(25).ToList();

        room.StartGame(selectedWords, starterId);

        await _roomRepo.SaveChangesAsync();
    }
}