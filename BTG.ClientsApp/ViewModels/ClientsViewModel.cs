using BTG.ClientsApp.Models;
using BTG.ClientsApp.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BTG.ClientsApp.ViewModels;

public partial class ClientsViewModel : BaseViewModel
{
    private readonly IClientRepository _repo;
    private readonly IWindowService _windows;
    private readonly IAlertService _alerts;

    public ObservableCollection<Client> Clients { get; } = new();

    public ClientsViewModel(IClientRepository repo, IWindowService windows, IAlertService alerts)
    {
        _repo = repo;
        _windows = windows;
        _alerts = alerts;

        Title = "Clientes";
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            Clients.Clear();
            var list = await _repo.GetAllAsync();
            foreach (var c in list.OrderBy(c => c.FullName))
                Clients.Add(c);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task AddAsync()
    {
        var saved = await _windows.OpenClientFormAsync(null);
        if (saved is null) return;

        await _repo.AddAsync(saved);
        Clients.Add(saved);
        SortInPlace();
    }

    [RelayCommand]
    public async Task EditAsync(Client client)
    {
        var copy = client.Clone();
        var saved = await _windows.OpenClientFormAsync(copy);
        if (saved is null) return;

        client.Name = saved.Name;
        client.Lastname = saved.Lastname;
        client.Age = saved.Age;
        client.Address = saved.Address;

        await _repo.UpdateAsync(client);
        SortInPlace();
    }

    [RelayCommand]
    public async Task DeleteAsync(Client client)
    {
        var ok = await _alerts.ConfirmAsync("Exclusão de Registro", $"Deseja realmente excluir registro de {client.FullName}?", "Excluir", "Cancelar");
        if (!ok) return;

        await _repo.DeleteAsync(client);
        Clients.Remove(client);
    }

    private void SortInPlace()
    {
        var ordered = Clients.OrderBy(c => c.FullName).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            if (!ReferenceEquals(Clients[i], ordered[i]))
                Clients.Move(Clients.IndexOf(ordered[i]), i);
        }
    }
}
