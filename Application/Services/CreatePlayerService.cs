using CodenameApp.Domain;

public class CreatePlayerService
{
    private readonly IPlayerRepository _repo;

    public CreatePlayerService(IPlayerRepository repo)
    {
        _repo = repo;
    }

    public async Task<(Guid Id, string Token)> Execute(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 20)
            throw new ArgumentException("Name must be 1-20 characters");

        var player = new Player(name);
        await _repo.AddAsync(player);
        return (player.Id, player.Token);
    }
}