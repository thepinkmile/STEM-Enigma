using CodeBreakers.Enigma;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodeBreakers.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProcessInputCommand))]
    public partial string InputText { get; set; }  = "";

    [ObservableProperty]
    public partial string CypherText { get; set; } = "";

    [RelayCommand(CanExecute = nameof(CanProcessInput))]
    private void ProcessInput()
    {
        var machine = EnigmaMachine.CreateModelI();
        CypherText = machine.Encrypt(InputText ?? string.Empty);
    }

    private bool CanProcessInput()
    {
        return !string.IsNullOrWhiteSpace(InputText);
    }
}
