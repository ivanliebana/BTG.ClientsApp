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
using Microsoft.UI.Xaml;
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

        // Fecha com segurança: reabilita o owner ANTES, autoriza o close e só então fecha
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
                            // reabilita o owner ANTES de fechar (evita corrida no Closing)
                            nint owner = 0;
                            lock (s_lock)
                            {
                                if (s_ownerByChild.TryGetValue(childHwnd, out owner))
                                {
                                    s_ownerByChild.Remove(childHwnd);
                                }
                                s_canClose.Add(childHwnd); // autoriza fechamento
                            }

                            if (owner != 0)
                            {
                                try { EnableWindow(owner, true); } catch { /* ignore */ }
                            }
                        }
                    }
                }
                catch { /* ignore */ }
#endif
                if (win is not null)
                    Application.Current?.CloseWindow(win);
            });

            return Task.CompletedTask;
        }

        // Conecta VM.ShowAlertAsync -> page.DisplayAlert (alerta acima do form)
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
        // ===== Win32 interop / estilos =====
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

        private const long WS_SYSMENU = 0x00080000L; // botão X / menu do sistema
        private const long WS_MINIMIZEBOX = 0x00020000L;
        private const long WS_MAXIMIZEBOX = 0x00010000L;
        private const long WS_THICKFRAME = 0x00040000L; // redimensionável
        private const long WS_CAPTION = 0x00C00000L; // título + ícone
        private const long WS_DLGFRAME = 0x00400000L; // moldura de diálogo (sem título)

        private const long WS_EX_APPWINDOW = 0x00040000L;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        private static readonly object s_lock = new();
        private static readonly HashSet<nint> s_canClose = new();              // children autorizadas a fechar
        private static readonly Dictionary<nint, nint> s_ownerByChild = new(); // child->owner

        /// <summary>
        /// Modal real + borda de janela (sem título): owned, owner desabilitado, sem redimensionar,
        /// sem min/max/X, centraliza; só fecha programaticamente. Estável para abrir/fechar múltiplas vezes.
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

            // Dono = primeira janela (principal)
            var ownerWindow = Application.Current?.Windows?.FirstOrDefault();
            nint ownerHwnd = 0;
            try
            {
                if (ownerWindow?.Handler?.PlatformView is Microsoft.UI.Xaml.Window ownerNative)
                    ownerHwnd = WindowNative.GetWindowHandle(ownerNative);
            }
            catch { /* ignore */ }

            // 1) Owned + desabilita principal (modal real)
            if (ownerHwnd != 0)
            {
                try
                {
                    SetWindowLongPtr(childHwnd, GWLP_HWNDPARENT, ownerHwnd);
                    EnableWindow(ownerHwnd, false);
                    lock (s_lock) { s_ownerByChild[childHwnd] = ownerHwnd; }
                }
                catch { /* ignore */ }
            }

            // 2) Estado normal e foco no child
            try
            {
                ShowWindow(childHwnd, SW_SHOWNORMAL);
                SetForegroundWindow(childHwnd);
            }
            catch { /* ignore */ }

            // 3) Estilo: SEM título/botões/redimensionar; COM borda (estilo diálogo)
            try
            {
                var style = GetWindowLongPtr(childHwnd, GWL_STYLE).ToInt64();

                // remove caption e botões
                style &= ~WS_CAPTION;     // sem título/ícone
                style &= ~WS_THICKFRAME;  // sem resize
                style &= ~WS_MINIMIZEBOX; // sem minimizar
                style &= ~WS_MAXIMIZEBOX; // sem maximizar
                style &= ~WS_SYSMENU;     // sem botão X

                // mantém moldura de diálogo (borda de janela sem título)
                style |= WS_DLGFRAME;

                SetWindowLongPtr(childHwnd, GWL_STYLE, new nint(style));

                // evita ícone na taskbar
                var ex = GetWindowLongPtr(childHwnd, GWL_EXSTYLE).ToInt64();
                ex &= ~WS_EX_APPWINDOW;
                ex |= WS_EX_TOOLWINDOW;
                SetWindowLongPtr(childHwnd, GWL_EXSTYLE, new nint(ex));

                // aplica mudanças
                SetWindowPos(childHwnd, IntPtr.Zero, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED);
            }
            catch { /* ignore */ }

            // 4) Centraliza
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

                // 5) Controle de fechamento
                if (appWindow is not null)
                {
                    // a) Cancelar qualquer tentativa de fechar do usuário (Alt+F4, etc.)
                    appWindow.Closing += (s, e) =>
                    {
                        try
                        {
                            bool allowClose;
                            lock (s_lock) { allowClose = s_canClose.Contains(childHwnd); }

                            if (!allowClose)
                            {
                                e.Cancel = true;                // bloqueia fechamento do usuário
                                SetForegroundWindow(childHwnd); // mantém foco no modal
                            }
                            // quando allowClose==true, não reabilitamos owner aqui
                            // pois o CloseWindowSafe já reabilitou ANTES de fechar.
                        }
                        catch
                        {
                            e.Cancel = true;
                            SetForegroundWindow(childHwnd);
                        }
                    };

                    // b) Plano B: se por algum motivo a janela realmente fechar,
                    // garanta que o owner volte habilitado (evita “owner travado”)
                    // Hook em Closed do XAML e também em Destroying do AppWindow (quando disponível)
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
                        catch { /* ignore */ }
                    };
                }
            }
            catch { /* ignore */ }
        }
#endif
    }
}
