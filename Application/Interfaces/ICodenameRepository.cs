using CodenameApp.Domain;

namespace CodenameApp.Application.Interfaces;

public interface ICodenameRepository
{
    Task AddAsync(Codename codename);
    Task<Codename?> GetByIdAsync(Guid id);
    Task<List<Codename>> GetAllAsync();
}