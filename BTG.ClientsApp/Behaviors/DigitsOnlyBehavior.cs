using Microsoft.Maui.Controls;
using System.Linq;

namespace BTG.ClientsApp.Behaviors
{
    /// <summary>
    /// Restricts an Entry to accept only digits (0-9).
    /// Also filters pasted text.
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

            // If it's already just digits, it doesn't do anything
            if (newText.All(char.IsDigit))
                return;

            // Filters any non-numeric characters
            var filtered = new string(newText.Where(char.IsDigit).ToArray());

            // Avoid loops by setting only when necessary
            if (!string.Equals(filtered, newText))
                entry.Text = filtered;
        }
    }
}
