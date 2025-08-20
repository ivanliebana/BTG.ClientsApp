using BTG.ClientsApp.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using BTG.ClientsApp.Services.Interfaces;


#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.Maui.Platform;
#endif

namespace BTG.ClientsApp.Views;

public partial class ClientsPage : ContentPage
{
    private readonly IWindowService _windowService;

    public ClientsPage(ClientsViewModel vm, IWindowService windowService)
    {
        InitializeComponent();
        BindingContext = vm;
        _windowService = windowService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var vm = (ClientsViewModel)BindingContext;

        // Carrega só na primeira vez (ou quando não estiver carregando)
        if (!vm.IsBusy && vm.Clients.Count == 0)
            vm.LoadCommand.Execute(null);
    }

    private async void OnExitClicked(object? sender, EventArgs e)
    {
#if WINDOWS
        await _windowService.CloseApplicationAsync(this, askConfirm: true);
#else
        // opcional: ignorar ou avisar
#endif
    }
}