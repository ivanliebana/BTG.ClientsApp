using CommunityToolkit.Mvvm.ComponentModel;

namespace BTG.ClientsApp.Models;

public partial class Client : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string lastname = string.Empty;

    [ObservableProperty]
    private int age;

    [ObservableProperty]
    private string address = string.Empty;

    public string FullName =>
        string.IsNullOrWhiteSpace(Lastname) ? Name?.Trim() ?? "" : $"{Name} {Lastname}".Trim();

    public Client Clone() => new()
    {
        Name = Name,
        Lastname = Lastname,
        Age = Age,
        Address = Address
    };
}
