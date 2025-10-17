using CodeBreakers.Enigma;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodeBreakers.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly EnigmaMachine _enigmaMachine = EnigmaMachine.CreateModelI();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProcessInputCommand))]
    public partial string InputText { get; set; }  = "";

    [ObservableProperty]
    public partial string CypherText { get; set; } = "";

    public Rotor LeftRotor => _enigmaMachine.LeftRotor;
    public Rotor MiddleRotor => _enigmaMachine.MiddleRotor;
    public Rotor RightRotor => _enigmaMachine.RightRotor;


    [RelayCommand(CanExecute = nameof(CanProcessInput))]
    private void ProcessInput()
    {
        CypherText = _enigmaMachine.Encrypt(InputText ?? string.Empty);
    }

    private bool CanProcessInput()
    {
        return !string.IsNullOrWhiteSpace(InputText);
    }
}
