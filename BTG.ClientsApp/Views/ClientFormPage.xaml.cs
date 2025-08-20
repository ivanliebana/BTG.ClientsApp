using BTG.ClientsApp.ViewModels;
using Microsoft.Maui.Controls;
using System.Linq;

namespace BTG.ClientsApp.Views;

public partial class ClientFormPage : ContentPage
{
    public ClientFormPage(ClientFormViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    public void CloseWindowSafe()
    {
        Dispatcher.Dispatch(() =>
        {
            var win = this.Window
                      ?? Application.Current?.Windows?.FirstOrDefault(w => w.Page == this);

            if (win is not null)
                Application.Current?.CloseWindow(win);
        });
    }
}
