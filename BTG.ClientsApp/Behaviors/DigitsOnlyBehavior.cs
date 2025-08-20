using Microsoft.Maui.Controls;
using System.Linq;

namespace BTG.ClientsApp.Behaviors
{
    /// <summary>
    /// Restringe um Entry para aceitar apenas dígitos (0-9).
    /// Filtra também texto colado.
    /// </summary>
    public sealed class DigitsOnlyBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry bindable)
        {
            base.OnAttachedTo(bindable);
            bindable.TextChanged += OnTextChanged;
        }

        protected override void OnDetachingFrom(Entry bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.TextChanged -= OnTextChanged;
        }

        private static void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not Entry entry) return;

            var newText = e.NewTextValue ?? string.Empty;

            // Se já são apenas dígitos, não faz nada
            if (newText.All(char.IsDigit))
                return;

            // Filtra qualquer caractere não numérico
            var filtered = new string(newText.Where(char.IsDigit).ToArray());

            // Evita loops setando apenas quando necessário
            if (!string.Equals(filtered, newText))
                entry.Text = filtered;
        }
    }
}
