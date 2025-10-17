namespace CodeBreakers.Enigma;

/// <summary>
/// Represents a rotor in the Enigma machine with configurable wiring and rotation.
/// </summary>
public class Rotor
{
    private readonly string _wiring;
    private readonly char _notch;
    private int _position;
    private int _ringSetting;

    /// <summary>
    /// Gets or sets the current position of the rotor (0-25).
    /// </summary>
    public int Position
    {
        get => _position;
        set => _position = value % 26;
    }

    /// <summary>
    /// Gets or sets the ring setting of the rotor (0-25).
    /// </summary>
    public int RingSetting
    {
        get => _ringSetting;
        set => _ringSetting = value % 26;
    }

    /// <summary>
    /// Initializes a new rotor with the specified wiring, notch position, initial position, and ring setting.
    /// </summary>
    /// <param name="wiring">The wiring configuration (26 unique letters).</param>
    /// <param name="notch">The notch letter that triggers the next rotor to step.</param>
    /// <param name="position">Initial rotor position (0-25 or A-Z).</param>
    /// <param name="ringSetting">Ring setting (0-25 or A-Z).</param>
    public Rotor(string wiring, char notch, int position = 0, int ringSetting = 0)
    {
        if (string.IsNullOrEmpty(wiring) || wiring.Length != 26)
            throw new ArgumentException("Wiring must contain exactly 26 characters.", nameof(wiring));

        _wiring = wiring.ToUpper();
        _notch = char.ToUpper(notch);
        Position = position;
        RingSetting = ringSetting;
    }

    /// <summary>
    /// Passes a signal forward through the rotor (right to left).
    /// </summary>
    public int Forward(int input)
    {
        int shift = Position - RingSetting;
        int index = Mod26(input + shift);
        int output = _wiring[index] - 'A';
        return Mod26(output - shift);
    }

    /// <summary>
    /// Passes a signal backward through the rotor (left to right).
    /// </summary>
    public int Backward(int input)
    {
        int shift = Position - RingSetting;
        int shiftedInput = Mod26(input + shift);
        char letter = (char)('A' + shiftedInput);
        int index = _wiring.IndexOf(letter);
        return Mod26(index - shift);
    }

    /// <summary>
    /// Rotates the rotor by one position.
    /// </summary>
    public void Step()
    {
        Position = (Position + 1) % 26;
    }

    /// <summary>
    /// Checks if the rotor is at its notch position (should trigger next rotor).
    /// </summary>
    public bool IsAtNotch()
    {
        return (char)('A' + Position) == _notch;
    }

    private static int Mod26(int value)
    {
        return ((value % 26) + 26) % 26;
    }

    /// <summary>
    /// Historical Enigma I rotor configurations.
    /// </summary>
    public static class RotorType
    {
        public static Rotor I(int position = 0, int ringSetting = 0) =>
            new("EKMFLGDQVZNTOWYHXUSPAIBRCJ", 'Q', position, ringSetting);

        public static Rotor II(int position = 0, int ringSetting = 0) =>
            new("AJDKSIRUXBLHWTMCQGZNPYFVOE", 'E', position, ringSetting);

        public static Rotor III(int position = 0, int ringSetting = 0) =>
            new("BDFHJLCPRTXVZNYEIWGAKMUSQO", 'V', position, ringSetting);

        public static Rotor IV(int position = 0, int ringSetting = 0) =>
            new("ESOVPZJAYQUIRHXLNFTGKDCMWB", 'J', position, ringSetting);

        public static Rotor V(int position = 0, int ringSetting = 0) =>
            new("VZBRGITYUPSDNHLXAWMJQOFECK", 'Z', position, ringSetting);
    }
}
