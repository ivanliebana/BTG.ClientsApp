using BTG.ClientsApp.ViewModels;
using Microsoft.Maui.Controls;

namespace BTG.ClientsApp.Views;

public partial class ClientsPage : ContentPage
{
    public ClientsPage(ClientsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm; // <— garante o VM
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var vm = (ClientsViewModel)BindingContext;

        // Carrega só na primeira vez (ou quando não estiver carregando)
        if (!vm.IsBusy && vm.Clients.Count == 0)
            vm.LoadCommand.Execute(null); // chama seu [RelayCommand] LoadAsync()
    }
}
