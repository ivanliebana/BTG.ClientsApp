using BTG.ClientsApp.Models;
using System.Threading.Tasks;

#if WINDOWS
using Microsoft.Maui.Controls;
#endif

namespace BTG.ClientsApp.Services.Interfaces;

public interface IWindowService
{
    Task<Client?> OpenClientFormAsync(Client? existing);

    // Closes the application with confirmation (Windows-only)
    Task CloseApplicationAsync(Page? anchor = null, bool askConfirm = true);
}
