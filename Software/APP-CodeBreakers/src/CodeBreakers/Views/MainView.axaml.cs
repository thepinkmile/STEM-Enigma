using Avalonia.Controls;
using Avalonia.Interactivity;

using CodeBreakers.ViewModels;

namespace CodeBreakers.Views;

public partial class MainView : UserControl
{
    internal async Task VerifyViewToggleAsync()
    {
        var tabs = (TabControl)Content!;
        var lab = (MachineLabView)((TabItem)tabs.Items[1]!).Content!;
        var model = (MainViewModel)DataContext!;
        tabs.SelectedIndex = 0;
        await Task.Delay(250);
        if (lab.IsRendererActive)
            throw new InvalidOperationException("The hidden 3D view is still rendering.");
        model.InputText = "A";
        model.ProcessInputCommand.Execute(null);
        if (model.CypherText.Length != 1)
            throw new InvalidOperationException("The original simulator stopped encoding after switching views.");
        tabs.SelectedIndex = 1;
        await Task.Delay(250);
        if (!lab.IsRendererActive || !lab.IsSynchronized)
            throw new InvalidOperationException("The 3D view did not resume with the simulator's state.");
    }

    // This constructor is used when the view is created by the XAML Previewer
    public MainView()
    {
        InitializeComponent();
    }

    private Func<MainViewModel>? ViewModelAccessor { get; }

    // This constructor is used when the view is created via dependency injection
    public MainView(Func<MainViewModel> viewModelAccessor)
        : this()
    {
        ViewModelAccessor = viewModelAccessor;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        // NB: This intentionally defers creating the view model until after the main view is loaded.
        // This is needed when the view model takes a dependency that requires the application lifetime to initialized.
        // Such as the IStorageProvider.
        DataContext ??= ViewModelAccessor?.Invoke();
        if (!OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS() &&
            !Design.IsDesignMode && Content is not TabControl && DataContext is MainViewModel model)
        {
            var simulator = Content;
            Content = null;
            var views = new TabControl
            {
                Items =
                {
                    new TabItem { Header = "Simulator", Content = simulator },
                    new TabItem { Header = "3D machine", Content = new MachineLabView(model) }
                },
                SelectedIndex = 0
            };
            Content = views;
            if (MachineLabView.SmokeTest)
            {
                model.InputText = "HELLOWORLD";
                model.ProcessInputCommand.Execute(null);
                if (model.CypherText != "ILBDAAMTAZ")
                    throw new InvalidOperationException("The original simulator failed its reference vector.");
                model.Machine.Configure("AAA", "AAA", "");
                model.NotifyMachineReset();
                model.InputText = model.CypherText = "";
                views.SelectedIndex = 1;
            }
        }
    }
}
