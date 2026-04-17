using CodenameApp.Application.Interfaces;
using CodenameApp.Domain;

namespace CodenameApp.Application.Services;

public class CreateCodenameService
{
    private readonly ICodenameRepository _repo;

    public CreateCodenameService(ICodenameRepository repo)
    {
        _repo = repo;
    }

    public async Task<Guid> Execute(string name, string desc)
    {
        var codename = new Codename(name, desc);
        await _repo.AddAsync(codename);
        return codename.Id;
    }
}