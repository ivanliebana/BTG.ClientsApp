using BTG.ClientsApp.Models;
using System.Threading.Tasks;

namespace BTG.ClientsApp.Services.Interfaces;

public interface IWindowService
{
    Task<Client?> OpenClientFormAsync(Client? existing);
}
