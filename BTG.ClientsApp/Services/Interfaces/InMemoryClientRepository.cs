using BTG.ClientsApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BTG.ClientsApp.Services.Interfaces;

public class InMemoryClientRepository : IClientRepository
{
    private readonly List<Client> _clients = new()
    {
        new Client { Name = "Alice", Lastname = "Silva", Age = 28, Address = "Rua A, 123" },
        new Client { Name = "Bruno", Lastname = "Oliveira", Age = 35, Address = "Rua B, 456" },
    };

    public Task<IReadOnlyList<Client>> GetAllAsync()
        => Task.FromResult((IReadOnlyList<Client>)[.. _clients]);

    public Task AddAsync(Client client)
    {
        _clients.Add(client);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Client client) => Task.CompletedTask;

    public Task DeleteAsync(Client client)
    {
        _clients.Remove(client);
        return Task.CompletedTask;
    }
}
