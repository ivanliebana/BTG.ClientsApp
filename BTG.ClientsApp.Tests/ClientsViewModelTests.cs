using BTG.ClientsApp.Models;
using BTG.ClientsApp.Services.Interfaces;
using BTG.ClientsApp.ViewModels;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BTG.ClientsApp.Tests;

public class ClientsViewModelTests
{
    // Repo fake em memória
    private class InMemRepo : IClientRepository
    {
        private readonly ObservableCollection<Client> _data = new()
        {
            new Client { Name = "Ana", Lastname = "Lima", Age = 30, Address = "Rua A, 100" },
            new Client { Name = "Bruno", Lastname = "Melo", Age = 28, Address = "Av. B, 200" }
        };

        public Task AddAsync(Client client) { _data.Add(client); return Task.CompletedTask; }
        public Task DeleteAsync(Client client) { _data.Remove(client); return Task.CompletedTask; }
        public Task<IReadOnlyList<Client>> GetAllAsync() => Task.FromResult<IReadOnlyList<Client>>(_data.ToList());
        public Task UpdateAsync(Client client) => Task.CompletedTask;
    }

    private static ClientsViewModel MakeVm(
        IClientRepository? repo = null,
        IWindowService? windows = null,
        IAlertService? alerts = null)
    {
        repo ??= new InMemRepo();

        var winMock = new Mock<IWindowService>();
        windows ??= winMock.Object;

        var alertMock = new Mock<IAlertService>();
        alertMock.Setup(a => a.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                 .ReturnsAsync(true); // padrão: confirma "Excluir"
        alerts ??= alertMock.Object;

        return new ClientsViewModel(repo, windows, alerts);
    }

    [Fact]
    public async Task LoadAsync_Popula_Clients_Ordenado()
    {
        var vm = MakeVm();

        await vm.LoadAsync();

        vm.Clients.Should().HaveCount(2);
        vm.Clients.Select(c => c.FullName).Should().BeInAscendingOrder();
        vm.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_Adiciona_Cliente_Retornado_Pela_Janela()
    {
        // Arrange
        var repo = new InMemRepo();

        var win = new Mock<IWindowService>();
        win.Setup(w => w.OpenClientFormAsync(null))
           .ReturnsAsync(new Client { Name = "Carla", Lastname = "Souza", Age = 26, Address = "Rua C, 300" });

        var vm = MakeVm(repo, win.Object);

        await vm.LoadAsync();
        vm.Clients.Should().HaveCount(2);

        // Act
        await vm.AddAsync();

        // Assert
        vm.Clients.Should().HaveCount(3);
        vm.Clients.Any(c => c.FullName == "Carla Souza").Should().BeTrue();
    }

    [Fact]
    public async Task EditAsync_Atualiza_Cliente_E_Ordena()
    {
        var repo = new InMemRepo();
        var vm = MakeVm(repo);

        await vm.LoadAsync();
        var alvo = vm.Clients.First(c => c.Name == "Bruno");

        // Janela devolve cópia editada
        var win = new Mock<IWindowService>();
        win.Setup(w => w.OpenClientFormAsync(It.IsAny<Client>()))
           .ReturnsAsync(new Client { Name = "Zeca", Lastname = "Almeida", Age = 35, Address = "Rua Z, 999" });

        // reconstroi vm com janela mockada
        vm = new ClientsViewModel(repo, win.Object, MakeVm().GetType()
            .GetField("_alerts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(MakeVm()) as IAlertService);

        // preload
        await vm.LoadAsync();
        alvo = vm.Clients.First(c => c.Name == "Bruno");

        // Act
        await vm.EditAsync(alvo);

        // Assert
        alvo.FullName.Should().Be("Zeca Almeida");
        vm.Clients.Select(c => c.FullName).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task DeleteAsync_Remove_Cliente_Mediante_Confirmacao()
    {
        var repo = new InMemRepo();

        var alert = new Mock<IAlertService>();
        alert.Setup(a => a.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), "Excluir", "Cancelar"))
             .ReturnsAsync(true);

        var vm = MakeVm(repo, alerts: alert.Object);

        await vm.LoadAsync();
        var countAntes = vm.Clients.Count;
        var alvo = vm.Clients.First();

        await vm.DeleteAsync(alvo);

        vm.Clients.Should().HaveCount(countAntes - 1);
        repo.GetAllAsync().Result.Any(c => ReferenceEquals(c, alvo)).Should().BeFalse();
    }
}
