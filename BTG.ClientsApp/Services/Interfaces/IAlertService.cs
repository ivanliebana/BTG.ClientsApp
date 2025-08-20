using System.Threading.Tasks;

namespace BTG.ClientsApp.Services.Interfaces;

public interface IAlertService
{
    Task<bool> ConfirmAsync(string title, string message, string accept = "Sim", string cancel = "Não");
    Task InfoAsync(string title, string message, string ok = "OK");
}
