using Avalonia;
using Avalonia.Controls;

using CodeBreakers.Enigma;

namespace CodeBreakers.Controls;

public partial class RotorControl : UserControl
{
    public static readonly StyledProperty<Rotor?> RotorProperty =
            AvaloniaProperty.Register<RotorControl, Rotor?>(nameof(Rotor));

    public Rotor? Rotor
    {
        get => GetValue(RotorProperty);
        set => SetValue(RotorProperty, value);
    }

    public RotorControl()
    {
        InitializeComponent();

        RotorProperty.Changed.Subscribe(_ =>
        {
            if (Rotor is { } rotor)
            {
                rotor.PositionChanged += Rotor_PositionChanged;
                SetCurrentPosition();
            }
        });
        ButtonSpinner.Spin += ButtonSpinner_Spin;
    }

    private void Rotor_PositionChanged(object? sender, EventArgs e)
    {
        SetCurrentPosition();
    }

    private void ButtonSpinner_Spin(object? sender, SpinEventArgs e)
    {
        if (Rotor is { } rotor)
        {
            e.Handled = true;
            if (e.Direction == SpinDirection.Increase)
            {
                rotor.Step();
            }
            else if (e.Direction == SpinDirection.Decrease)
            {
                rotor.Position += 25;
            }
            SetCurrentPosition();
        }
    }

    private void SetCurrentPosition()
    {
        if (Rotor is { } rotor)
        {
            ButtonSpinner.Content = (char)('A' + rotor.Position);
        }
    }
}