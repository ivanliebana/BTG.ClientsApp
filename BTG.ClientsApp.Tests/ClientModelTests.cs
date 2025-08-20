using BTG.ClientsApp.Models;
using FluentAssertions;
using System.ComponentModel;

namespace BTG.ClientsApp.Tests;

public class ClientModelTests
{
    [Fact]
    public void Alterar_Name_ou_Lastname_Notifica_FullName()
    {
        var c = new Client { Name = "Ana", Lastname = "Lima" };
        string? lastNotified = null;

        ((INotifyPropertyChanged)c).PropertyChanged += (_, e) => lastNotified = e.PropertyName;

        c.Name = "Maria";
        lastNotified.Should().BeOneOf(nameof(Client.Name), nameof(Client.FullName));

        c.Lastname = "Souza";
        lastNotified.Should().BeOneOf(nameof(Client.Lastname), nameof(Client.FullName));

        c.FullName.Should().Be("Maria Souza");
    }
}
