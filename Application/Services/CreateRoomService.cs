using CodenameApp.Application.Interfaces;
using CodenameApp.Domain;

public class CreateRoomService
{
    private readonly IGameRoomRepository _repo;

    public CreateRoomService(IGameRoomRepository repo)
    {
        _repo = repo;
    }

    public async Task<string> Execute()
    {
        var code = GenerateCode();
        var room = new GameRoom(code);

        await _repo.AddAsync(room);
        return code;
    }

    private string GenerateCode()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
    }
}