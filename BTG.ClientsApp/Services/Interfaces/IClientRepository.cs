using BTG.ClientsApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BTG.ClientsApp.Services.Interfaces;

public interface IClientRepository
{
    Task<IReadOnlyList<Client>> GetAllAsync();
    Task AddAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(Client client);
}
