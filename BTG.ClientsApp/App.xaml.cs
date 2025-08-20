using BTG.ClientsApp.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace BTG.ClientsApp;

public partial class App : Application
{
    private readonly ClientsPage _rootPage;

    public App(ClientsPage rootPage)
    {
        InitializeComponent();
        _rootPage = rootPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Wrap in NavigationPage so ToolbarItem renders on Windows
        var nav = new NavigationPage(_rootPage)
        {
            BarBackgroundColor = Colors.Silver,
            BarTextColor = Colors.Black
        };

        return new Window { Page = nav };
    }
}
