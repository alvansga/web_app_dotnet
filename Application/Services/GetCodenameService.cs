using CodenameApp.Application.Interfaces;
using CodenameApp.Domain;

namespace CodenameApp.Application.Services;

public class GetCodenameService
{
    private readonly ICodenameRepository _repo;

    public GetCodenameService(ICodenameRepository repo)
    {
        _repo = repo;
    }

    public async Task<Codename?> Execute(Guid id)
    {
        return await _repo.GetByIdAsync(id);
    }
}