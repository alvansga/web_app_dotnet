using CodenameApp.Domain;

public class CreatePlayerService
{
    private readonly IPlayerRepository _repo;

    public CreatePlayerService(IPlayerRepository repo)
    {
        _repo = repo;
    }

    public async Task<Guid> Execute(string name)
    {
        var player = new Player(name);
        await _repo.AddAsync(player);
        return player.Id;
    }
}