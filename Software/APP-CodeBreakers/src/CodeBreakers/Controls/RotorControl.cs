using Avalonia;
using Avalonia.Controls;

using CodeBreakers.Enigma;

namespace CodeBreakers.Controls;

//public class RotorControl : ButtonSpinner
//{
//    public static readonly StyledProperty<Rotor?> RotorProperty =
//            AvaloniaProperty.Register<RotorControl, Rotor?>(nameof(Rotor));

//    public Rotor? Rotor
//    {
//        get => GetValue(RotorProperty); 
//        set => SetValue(RotorProperty, value);
//    }

//    public RotorControl()
//    {

//    }

//    protected override void OnSpin(SpinEventArgs e)
//    {
//        if (Rotor is { } rotor)
//        {
//            if (e.Direction == SpinDirection.Increase)
//            {
//                rotor.Step();
//            }
//            else if (e.Direction == SpinDirection.Decrease)
//            {
//                rotor.Position += 25;
//            }
//            Content = rotor.PositionCharacter;
//        }

//        e.Handled = true;
//        base.OnSpin(e);
//    }
//}
