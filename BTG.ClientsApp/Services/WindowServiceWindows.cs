using BTG.ClientsApp.Models;
using BTG.ClientsApp.Services.Interfaces;
using BTG.ClientsApp.ViewModels;
using BTG.ClientsApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Window = Microsoft.Maui.Controls.Window;
using Application = Microsoft.Maui.Controls.Application;

#if WINDOWS
using Microsoft.UI.Windowing;
using WinRT.Interop;
using System.Runtime.InteropServices;
#endif

namespace BTG.ClientsApp.Services
{
    public class WindowServiceWindows : IWindowService
    {
        private readonly IServiceProvider _sp;

        private const int DefaultWidth = 640;
        private const int DefaultHeight = 440;

        public WindowServiceWindows(IServiceProvider sp) => _sp = sp;

        public Task<Client?> OpenClientFormAsync(Client? existing)
        {
            var tcs = new TaskCompletionSource<Client?>();

            var page = _sp.GetRequiredService<ClientFormPage>();
            var vm = _sp.GetRequiredService<ClientFormViewModel>();
            page.BindingContext = vm;

            // Alerts de validação ancorados no FORM
            TryBindVmAlertToPage(vm, page);

            vm.Load(existing);

            // Únicos caminhos para fechar (programático)
            vm.Saved += async (_, client) =>
            {
                tcs.TrySetResult(client);
                await CloseWindowSafe(page);
            };
            vm.Canceled += async (_, __) =>
            {
                tcs.TrySetResult(null);
                await CloseWindowSafe(page);
            };

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var window = new Window(page)
                {
                    Title = string.Empty, // sem caption
                    Width = DefaultWidth,
                    Height = DefaultHeight
                };

#if WINDOWS
                window.Created += (_, __) => PrepareModalOwnedWindow_WithBorder(window, DefaultWidth, DefaultHeight);
#endif
                Application.Current?.OpenWindow(window);
            });

            return tcs.Task;
        }

        // Closes safely: reactivates the owner BEFORE, authorizes the close and only then closes
        private static Task CloseWindowSafe(Page page)
        {
            page.Dispatcher.Dispatch(() =>
            {
                var win = page.Window
                          ?? Application.Current?.Windows?.FirstOrDefault(w => w.Page == page);

#if WINDOWS
                try
                {
                    if (win?.Handler?.PlatformView is Microsoft.UI.Xaml.Window native)
                    {
                        var childHwnd = WindowNative.GetWindowHandle(native);
                        if (childHwnd != 0)
                        {
                            // rehabilitates the owner BEFORE closing (avoids race on Closing)
                            nint owner = 0;
                            lock (s_lock)
                            {
                                if (s_ownerByChild.TryGetValue(childHwnd, out owner))
                                {
                                    s_ownerByChild.Remove(childHwnd);
                                }
                                s_canClose.Add(childHwnd); // authorizes closure
                            }

                            if (owner != 0)
                            {
                                try { EnableWindow(owner, true); } catch { }
                            }
                        }
                    }
                }
                catch { }
#endif
                if (win is not null)
                    Application.Current?.CloseWindow(win);
            });

            return Task.CompletedTask;
        }

        // Connect VM.ShowAlertAsync -> page.DisplayAlert (alert above the form)
        private static void TryBindVmAlertToPage(ClientFormViewModel vm, Page page)
        {
            var prop = vm.GetType().GetProperty("ShowAlertAsync");
            if (prop != null && prop.PropertyType == typeof(Func<string, string, Task>))
            {
                var del = new Func<string, string, Task>((title, message) =>
                    page.DisplayAlert(title, message, "OK"));
                prop.SetValue(vm, del);
            }
        }

#if WINDOWS
        // ===== Win32 interop / styles =====
        [DllImport("user32.dll")] private static extern bool ShowWindow(nint hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern bool EnableWindow(nint hWnd, bool bEnable);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hWnd);
        [DllImport("user32.dll", SetLastError = true)] private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);
        [DllImport("user32.dll", SetLastError = true)] private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int SW_SHOWNORMAL = 1;

        private const int GWL_STYLE = -16;
        private const int GWLP_HWNDPARENT = -8;
        private const int GWL_EXSTYLE = -20;

        private const long WS_SYSMENU = 0x00080000L; // X button / system menu
        private const long WS_MINIMIZEBOX = 0x00020000L;
        private const long WS_MAXIMIZEBOX = 0x00010000L;
        private const long WS_THICKFRAME = 0x00040000L; // resizable
        private const long WS_CAPTION = 0x00C00000L; // title + icon
        private const long WS_DLGFRAME = 0x00400000L; // dialog frame (untitled)

        private const long WS_EX_APPWINDOW = 0x00040000L;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        private static readonly object s_lock = new();
        private static readonly HashSet<nint> s_canClose = new();              // children authorized to close
        private static readonly Dictionary<nint, nint> s_ownerByChild = new(); // child->owner

        /// <summary>
        /// Real modal + window border (untitled): owned, owner disabled, no resizing,
        /// without min/max/X, center; it just closes programmatically. Stable to open/close multiple times.
        /// </summary>
        private static void PrepareModalOwnedWindow_WithBorder(Window childWindow, int width, int height)
        {
            Microsoft.UI.Xaml.Window? childNative = null;
            nint childHwnd = 0;

            try
            {
                childNative = (Microsoft.UI.Xaml.Window)childWindow.Handler!.PlatformView;
                childHwnd = WindowNative.GetWindowHandle(childNative);
            }
            catch { return; }

            // Owner = first window (main)
            var ownerWindow = Application.Current?.Windows?.FirstOrDefault();
            nint ownerHwnd = 0;
            try
            {
                if (ownerWindow?.Handler?.PlatformView is Microsoft.UI.Xaml.Window ownerNative)
                    ownerHwnd = WindowNative.GetWindowHandle(ownerNative);
            }
            catch { }

            // 1) Owned + disables main (real modal)
            if (ownerHwnd != 0)
            {
                try
                {
                    SetWindowLongPtr(childHwnd, GWLP_HWNDPARENT, ownerHwnd);
                    EnableWindow(ownerHwnd, false);
                    lock (s_lock) { s_ownerByChild[childHwnd] = ownerHwnd; }
                }
                catch { }
            }

            // 2) Normal state and focus on the child
            try
            {
                ShowWindow(childHwnd, SW_SHOWNORMAL);
                SetForegroundWindow(childHwnd);
            }
            catch { }

            // 3) Estilo: SEM título/botões/redimensionar; COM borda (estilo diálogo)
            try
            {
                var style = GetWindowLongPtr(childHwnd, GWL_STYLE).ToInt64();

                // remove caption and buttons
                style &= ~WS_CAPTION;     // no title/icon
                style &= ~WS_THICKFRAME;  // no resize
                style &= ~WS_MINIMIZEBOX; // without minimizing
                style &= ~WS_MAXIMIZEBOX; // without maximizing
                style &= ~WS_SYSMENU;     // no X button

                // maintains dialog frame (untitled window border)
                style |= WS_DLGFRAME;

                SetWindowLongPtr(childHwnd, GWL_STYLE, new nint(style));

                // avoid icon in taskbar
                var ex = GetWindowLongPtr(childHwnd, GWL_EXSTYLE).ToInt64();
                ex &= ~WS_EX_APPWINDOW;
                ex |= WS_EX_TOOLWINDOW;
                SetWindowLongPtr(childHwnd, GWL_EXSTYLE, new nint(ex));

                // apply changes
                SetWindowPos(childHwnd, IntPtr.Zero, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED);
            }
            catch { }

            // 4) Centralize
            try
            {
                var childWindowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(childHwnd);
                var appWindow = AppWindow.GetFromWindowId(childWindowId);
                var display = DisplayArea.GetFromWindowId(childWindowId, DisplayAreaFallback.Nearest);
                var wa = display.WorkArea;

                int w = childWindow.Width != 0 ? (int)childWindow.Width : width;
                int h = childWindow.Height != 0 ? (int)childWindow.Height : height;
                int x = wa.X + (wa.Width - w) / 2;
                int y = wa.Y + (wa.Height - h) / 2;

                appWindow?.MoveAndResize(new Windows.Graphics.RectInt32(x, y, w, h));

                // 5) Closing control
                if (appWindow is not null)
                {
                    // a) Cancel any user close attempt (Alt+F4, etc.)
                    appWindow.Closing += (s, e) =>
                    {
                        try
                        {
                            bool allowClose;
                            lock (s_lock) { allowClose = s_canClose.Contains(childHwnd); }

                            if (!allowClose)
                            {
                                e.Cancel = true;                // blocks user closure
                                SetForegroundWindow(childHwnd); // keeps focus on the modal
                            }
                            // when allowClose==true, we do not re-enable owner here
                            // because CloseWindowSafe has already re-enabled BEFORE closing.
                        }
                        catch
                        {
                            e.Cancel = true;
                            SetForegroundWindow(childHwnd);
                        }
                    };

                    // b) Plan B: if for some reason the window actually closes,
                    // ensure that the owner is enabled again (avoids “owner stuck”)
                    // Hook in XAML Closed and also in AppWindow Destroying (when available)
                    childNative!.Closed += (_, __) =>
                    {
                        try
                        {
                            nint owner = 0;
                            lock (s_lock)
                            {
                                if (s_ownerByChild.TryGetValue(childHwnd, out owner))
                                    s_ownerByChild.Remove(childHwnd);
                                s_canClose.Remove(childHwnd);
                            }
                            if (owner != 0) EnableWindow(owner, true);
                        }
                        catch {}
                    };
                }
            }
            catch { }
        }

        public async Task CloseApplicationAsync(Page? anchor = null, bool askConfirm = true)
        {
#if WINDOWS
            try
            {
                // 1) Confirmation (optional)
                if (askConfirm)
                {
                    var page = anchor ?? Application.Current?.MainPage;
                    if (page != null)
                    {
                        var ok = await page.DisplayAlert("Sair", "Deseja realmente fechar a aplicação?", "Sim", "Cancelar");
                        if (!ok) return;
                    }
                }

                // 2) Rehabilitates any 'owned' window and authorizes closure (if you have modals opened by your service)
                try
                {
                    lock (s_lock)
                    {
                        foreach (var kv in s_ownerByChild.ToList())
                        {
                            var child = kv.Key;
                            var owner = kv.Value;
                            try { EnableWindow(owner, true); } catch { }
                            s_canClose.Add(child);
                            s_ownerByChild.Remove(child);
                        }
                    }
                }
                catch {}

                // 3) Close ALL MAUI windows cleanly via native (avoids heap corruption)
                var windows = Application.Current?.Windows?.ToList() ?? new List<Window>();
                foreach (var win in windows)
                {
                    try
                    {
                        if (win?.Handler?.PlatformView is Microsoft.UI.Xaml.Window native)
                            native.Close(); // close WinUI Window
                        else
                            Application.Current?.CloseWindow(win); // fallback MAUI
                    }
                    catch { }
                }

                // 4)If there is any left (rare), close the main one last
                var main = Application.Current?.Windows?.FirstOrDefault();
                if (main?.Handler?.PlatformView is Microsoft.UI.Xaml.Window mainNative)
                    mainNative.Close();
                else if (main is not null)
                    Application.Current?.CloseWindow(main);
#else
            // Other platforms: optionally ignore
            await Task.CompletedTask;
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao fechar aplicação: {ex}");
            }
        }
#endif
    }
}
