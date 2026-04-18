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

    public async Task Execute(string code)
    {
        var room = await _roomRepo.GetByCodeAsync(code);
        if (room == null) throw new Exception("Room not found");

        var codenames = await _codenameRepo.GetAllAsync();
        var words = codenames.Select(c => c.Name).ToList();

        if (words.Count < 25) 
        {
            var fallback = new List<string> {
                "APPLE", "BANANA", "CHERRY", "DOG", "ELEPHANT", "FOX", "GRAPE", "HORSE", "IGLOO", "JUMP",
                "KITE", "LION", "MOUSE", "NIGHT", "OWL", "PIG", "QUEEN", "RABBIT", "SNAKE", "TIGER",
                "UMBRELLA", "VAN", "WHALE", "XYLOPHONE", "YACHT", "ZEBRA", "GUITAR", "PIANO", "MOON", "SUN"
            };
            words.AddRange(fallback.Where(f => !words.Contains(f)));
        }

        var random = new Random();
        var selectedWords = words.OrderBy(x => random.Next()).Take(25).ToList();

        room.StartGame(selectedWords);

        await _roomRepo.SaveChangesAsync();
    }
}