using System.Threading.Tasks;

namespace BTG.ClientsApp.Services.Interfaces;

public interface IAlertService
{
    Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
    Task InfoAsync(string title, string message, string ok = "OK");
}
