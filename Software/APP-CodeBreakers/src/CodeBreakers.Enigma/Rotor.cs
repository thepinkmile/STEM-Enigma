namespace CodeBreakers.Enigma;

/// <summary>
/// Represents a rotor in the Enigma machine with configurable wiring and rotation.
/// </summary>
public class Rotor
{
    public event EventHandler? PositionChanged;

    private readonly int[] _wiring;
    private readonly int _notch;
    private int _position;
    private int _ringSetting;

    public int WiringLength => _wiring.Length;

    /// <summary>
    /// Gets or sets the current position of the rotor.
    /// </summary>
    public int Position
    {
        get => _position;
        set
        {
            var newValue = value % WiringLength;
            if (newValue != _position)
            {
                _position = newValue;
                PositionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets or sets the ring setting of the rotor.
    /// </summary>
    public int RingSetting
    {
        get => _ringSetting;
        set => _ringSetting = value % WiringLength;
    }

    /// <summary>
    /// Initializes a new rotor with the specified wiring, notch position, initial position, and ring setting.
    /// </summary>
    /// <param name="wiring">The wiring configuration.</param>
    /// <param name="notch">The notch letter that triggers the next rotor to step.</param>
    /// <param name="position">Initial rotor position.</param>
    /// <param name="ringSetting">Ring setting.</param>
    public Rotor(int[] wiring, int notch, int position = 0, int ringSetting = 0)
    {
        ArgumentNullException.ThrowIfNull(wiring);
        if (wiring.Any(x => x < 0 || x >= wiring.Length))
            throw new ArgumentException("Wiring values must be between 0 and the length of the wiring", nameof(wiring));

        ArgumentOutOfRangeException.ThrowIfLessThan(notch, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(notch, wiring.Length);

        ArgumentOutOfRangeException.ThrowIfLessThan(position, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, wiring.Length);

        ArgumentOutOfRangeException.ThrowIfLessThan(ringSetting, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(ringSetting, wiring.Length);

        _wiring = wiring;
        _notch = notch;
        Position = position;
        RingSetting = ringSetting;
    }

    /// <summary>
    /// Passes a signal forward through the rotor (right to left).
    /// </summary>
    public int Forward(int input)
    {
        int shift = Position - RingSetting;
        int index = Mod(input + shift);
        int output = _wiring[index];
        return Mod(output - shift);
    }

    /// <summary>
    /// Passes a signal backward through the rotor (left to right).
    /// </summary>
    public int Backward(int input)
    {
        int shift = Position - RingSetting;
        int shiftedInput = Mod(input + shift);
        int index = _wiring.IndexOf(shiftedInput);
        return Mod(index - shift);
    }

    /// <summary>
    /// Rotates the rotor by one position.
    /// </summary>
    public void Step()
    {
        Position = (Position + 1) % WiringLength;
    }

    /// <summary>
    /// Checks if the rotor is at its notch position (should trigger next rotor).
    /// </summary>
    public bool IsAtNotch()
    {
        return Position == _notch;
    }

    private int Mod(int value)
    {
        //NB: The additional + % is to handle the negative value case and ensures the result is always [0, WiringLength]
        return ((value % WiringLength) + WiringLength) % WiringLength;
    }

    /// <summary>
    /// Historical Enigma I rotor configurations.
    /// </summary>
    public static class EnigmaI
    {
        private static Rotor FromCharacterSet(string characterMapping, int notch, int position, int ringSet)
        {
            int[] wiring = KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, characterMapping);
            return new Rotor(wiring, notch, position, ringSet);
        }

        public static Rotor I(int position = 0, int ringSetting = 0) =>
            FromCharacterSet("EKMFLGDQVZNTOWYHXUSPAIBRCJ", 16 /*Q*/, position, ringSetting);

        public static Rotor II(int position = 0, int ringSetting = 0) =>
            FromCharacterSet("AJDKSIRUXBLHWTMCQGZNPYFVOE", 4 /*E*/, position, ringSetting);

        public static Rotor III(int position = 0, int ringSetting = 0) =>
            FromCharacterSet("BDFHJLCPRTXVZNYEIWGAKMUSQO", 21 /*V*/, position, ringSetting);

        public static Rotor IV(int position = 0, int ringSetting = 0) =>
            FromCharacterSet("ESOVPZJAYQUIRHXLNFTGKDCMWB", 9 /*J*/, position, ringSetting);

        public static Rotor V(int position = 0, int ringSetting = 0) =>
            FromCharacterSet("VZBRGITYUPSDNHLXAWMJQOFECK", 25 /*Z*/, position, ringSetting);
    }
}
