using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BTG.ClientsApp.Models;
using Microsoft.Maui.Controls;

namespace BTG.ClientsApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Add/Edit client modal form window.
    /// Comments are in English per the project convention.
    /// </summary>
    public partial class ClientFormViewModel : ObservableObject
    {
        private Client? _original;

        /// <summary>
        /// Hooked by WindowServiceWindows to page.DisplayAlert(...)
        /// so alerts appear on top of the FORM window (not the main window).
        /// </summary>
        public Func<string, string, Task>? ShowAlertAsync { get; set; }

        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private string lastname = string.Empty;
        [ObservableProperty] private string ageText = string.Empty;   // keep text to validate digits
        [ObservableProperty] private string address = string.Empty;

        public event EventHandler<Client>? Saved;
        public event EventHandler? Canceled;

        /// <summary>
        /// Loads an existing client into the form; if null, starts with empty fields.
        /// </summary>
        public void Load(Client? existing)
        {
            _original = existing;

            if (existing is not null)
            {
                Name = existing.Name;
                Lastname = existing.Lastname;
                AgeText = existing.Age.ToString();
                Address = existing.Address;
            }
            else
            {
                Name = Lastname = Address = string.Empty;
                AgeText = string.Empty;
            }
        }

        /// <summary>
        /// Cancel and close the modal (actual window closing is handled by WindowService).
        /// </summary>
        [RelayCommand]
        private void Cancel()
            => Canceled?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Validate and save. Emits Saved event with the mapped Client instance.
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            var (ok, errorMessage) = Validate();
            if (!ok)
            {
                await ShowValidationAsync("Validação", errorMessage!);
                return;
            }

            // Map to model
            _ = int.TryParse(AgeText, out var age); // safe because Validate() already checked

            var result = _original ?? new Client();
            result.Name = Name.Trim();
            result.Lastname = (Lastname ?? string.Empty).Trim();
            result.Age = age;
            result.Address = (Address ?? string.Empty).Trim();

            Saved?.Invoke(this, result);
        }

        /// <summary>
        /// Centralized validation rules for the form.
        /// </summary>
        private (bool ok, string? message) Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return (false, "O campo Nome é obrigatório.");

            if (string.IsNullOrWhiteSpace(Lastname))
                return (false, "O campo Sobrenome é obrigatório.");

            // Accept only digits in AgeText
            if (string.IsNullOrWhiteSpace(AgeText) || !AgeText.All(char.IsDigit))
                return (false, "Idade deve conter apenas números.");

            if (!int.TryParse(AgeText, out var age) || age < 18 || age > 120)
                return (false, "Idade deve ser um número válido entre 18 e 120.");

            if (string.IsNullOrWhiteSpace(Address))
                return (false, "O campo Endereço é obrigatório.");

            return (true, null);
        }

        /// <summary>
        /// Shows a validation dialog anchored to the FORM window.
        /// Falls back to last window's page if not wired.
        /// </summary>
        private async Task ShowValidationAsync(string title, string message)
        {
            if (ShowAlertAsync is not null)
            {
                await ShowAlertAsync(title, message);
                return;
            }

            // Fallback (should rarely happen if WindowService wired ShowAlertAsync properly)
            var page = Application.Current?.Windows?.Count > 0
                ? Application.Current.Windows[^1].Page
                : Application.Current?.Windows?.FirstOrDefault()?.Page;

            if (page is not null)
                await page.DisplayAlert(title, message, "OK");
        }
    }
}
