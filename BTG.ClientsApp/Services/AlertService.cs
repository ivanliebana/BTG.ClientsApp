using BTG.ClientsApp.Services.Interfaces;
using Microsoft.Maui.Controls;
using System.Linq;
using System.Threading.Tasks;

namespace BTG.ClientsApp.Services;

public class AlertService : IAlertService
{
    private static Page? GetActivePage()
        => Application.Current?.Windows?.FirstOrDefault()?.Page;

    public Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
        => GetActivePage()?.DisplayAlert(title, message, accept, cancel)
           ?? Task.FromResult(false);

    public Task InfoAsync(string title, string message, string ok = "OK")
        => GetActivePage()?.DisplayAlert(title, message, ok)
           ?? Task.CompletedTask;
}
