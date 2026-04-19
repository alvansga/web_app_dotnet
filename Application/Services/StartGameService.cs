using System.IO;
using System.Linq;
using CodenameApp.Application.Interfaces;

public class StartGameService
{
    private readonly IGameRoomRepository _roomRepo;
    private readonly ICodenameRepository _codenameRepo;

    public StartGameService(IGameRoomRepository roomRepo, ICodenameRepository codenameRepo)
    {
        _roomRepo = roomRepo;
        _codenameRepo = codenameRepo;
    }

    public async Task Execute(string code, Guid starterId)
    {
        var room = await _roomRepo.GetByCodeAsync(code);
        if (room == null) throw new Exception("Room not found");

        // Load words from words.txt
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "words.txt");
        // Fallback to project root if BaseDirectory is bin/Debug/...
        if (!File.Exists(filePath))
        {
            filePath = "words.txt";
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