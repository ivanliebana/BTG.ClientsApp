using BTG.ClientsApp.Services;
using BTG.ClientsApp.Services.Interfaces;
using BTG.ClientsApp.ViewModels;
using BTG.ClientsApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;

#if WINDOWS
using Microsoft.UI.Windowing;
using WinRT.Interop;
using System.Runtime.InteropServices;
#endif

namespace BTG.ClientsApp;

public static class MauiProgram
{
#if WINDOWS
    // Ensures only the first created window (main window) is maximized.
    private static bool s_mainWindowMaximized;

    // Win32 fallback for maximize
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);
    private const int SW_MAXIMIZE = 3;
#endif

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Dependency Injection
        builder.Services.AddSingleton<IClientRepository, InMemoryClientRepository>();
        builder.Services.AddSingleton<IAlertService, AlertService>();
        builder.Services.AddSingleton<IWindowService, WindowServiceWindows>();

        builder.Services.AddSingleton<ClientsViewModel>();
        builder.Services.AddTransient<ClientFormViewModel>();

        builder.Services.AddSingleton<ClientsPage>();
        builder.Services.AddTransient<ClientFormPage>();

        // Windows: maximize ONLY the first (main) window created
        builder.ConfigureLifecycleEvents(lifecycle =>
        {
#if WINDOWS
            lifecycle.AddWindows(w =>
            {
                w.OnWindowCreated(nativeWin =>
                {
                    // Only maximize the very first window (the main window).
                    if (s_mainWindowMaximized)
                        return;

                    s_mainWindowMaximized = true;

                    try
                    {
                        var hwnd = WindowNative.GetWindowHandle(nativeWin);
                        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                        var appWindow = AppWindow.GetFromWindowId(windowId);

                        // Prefer OverlappedPresenter (works across SDK versions)
                        if (appWindow?.Presenter is OverlappedPresenter presenter)
                        {
                            presenter.Maximize();
                        }
                        else
                        {
                            // Fallback Win32
                            ShowWindow(hwnd, SW_MAXIMIZE);
                        }
                    }
                    catch
                    {
                        var hwnd = WindowNative.GetWindowHandle(nativeWin);
                        ShowWindow(hwnd, SW_MAXIMIZE);
                    }
                });
            });
#endif
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
