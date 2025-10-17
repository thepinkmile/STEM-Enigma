namespace CodeBreakers.Enigma;

/// <summary>
/// Represents an Enigma Model I machine with three rotors, a reflector, and a plugboard.
/// </summary>
public class EnigmaMachine
{
    private readonly Rotor _leftRotor;
    private readonly Rotor _middleRotor;
    private readonly Rotor _rightRotor;
    private readonly Reflector _reflector;
    private readonly Plugboard _plugboard;

    /// <summary>
    /// Initializes a new Enigma Model I machine.
    /// </summary>
    /// <param name="leftRotor">The left rotor (slow).</param>
    /// <param name="middleRotor">The middle rotor.</param>
    /// <param name="rightRotor">The right rotor (fast).</param>
    /// <param name="reflector">The reflector.</param>
    /// <param name="plugboard">The plugboard (optional).</param>
    public EnigmaMachine(
        Rotor leftRotor,
        Rotor middleRotor,
        Rotor rightRotor,
        Reflector reflector,
        Plugboard? plugboard = null)
    {
        _leftRotor = leftRotor ?? throw new ArgumentNullException(nameof(leftRotor));
        _middleRotor = middleRotor ?? throw new ArgumentNullException(nameof(middleRotor));
        _rightRotor = rightRotor ?? throw new ArgumentNullException(nameof(rightRotor));
        _reflector = reflector ?? throw new ArgumentNullException(nameof(reflector));
        _plugboard = plugboard ?? new Plugboard();
    }

    /// <summary>
    /// Encrypts or decrypts a single character.
    /// </summary>
    /// <param name="input">The character to encrypt/decrypt (A-Z).</param>
    /// <returns>The encrypted/decrypted character.</returns>
    public char EncryptChar(char input)
    {
        input = char.ToUpper(input);

        // Only process letters A-Z
        if (input < 'A' || input > 'Z')
            return input;

        // Step rotors before encryption (double-stepping mechanism)
        StepRotors();

        // Convert to position (0-25)
        int position = input - 'A';

        // Through plugboard
        position = _plugboard.Swap(position);

        // Forward through rotors (right to left)
        position = _rightRotor.Forward(position);
        position = _middleRotor.Forward(position);
        position = _leftRotor.Forward(position);

        // Through reflector
        position = _reflector.Reflect(position);

        // Backward through rotors (left to right)
        position = _leftRotor.Backward(position);
        position = _middleRotor.Backward(position);
        position = _rightRotor.Backward(position);

        // Through plugboard again
        position = _plugboard.Swap(position);

        // Convert back to letter
        return (char)('A' + position);
    }

    /// <summary>
    /// Encrypts or decrypts a text string.
    /// </summary>
    /// <param name="text">The text to encrypt/decrypt.</param>
    /// <returns>The encrypted/decrypted text.</returns>
    public string Encrypt(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new char[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            result[i] = EncryptChar(text[i]);
        }

        return new string(result);
    }

    /// <summary>
    /// Steps the rotors according to the double-stepping mechanism.
    /// </summary>
    private void StepRotors()
    {
        // Double-stepping: if middle rotor is at notch, both middle and left rotors step
        if (_middleRotor.IsAtNotch())
        {
            _middleRotor.Step();
            _leftRotor.Step();
        }
        // If right rotor is at notch, middle rotor steps
        else if (_rightRotor.IsAtNotch())
        {
            _middleRotor.Step();
        }

        // Right rotor always steps
        _rightRotor.Step();
    }

    /// <summary>
    /// Resets the rotor positions to the specified settings.
    /// </summary>
    /// <param name="leftPosition">Left rotor position (0-25 or A-Z).</param>
    /// <param name="middlePosition">Middle rotor position (0-25 or A-Z).</param>
    /// <param name="rightPosition">Right rotor position (0-25 or A-Z).</param>
    public void SetRotorPositions(int leftPosition, int middlePosition, int rightPosition)
    {
        _leftRotor.Position = leftPosition;
        _middleRotor.Position = middlePosition;
        _rightRotor.Position = rightPosition;
    }

    /// <summary>
    /// Gets the current rotor positions as a string (e.g., "ABC").
    /// </summary>
    public string GetRotorPositions()
    {
        return $"{(char)('A' + _leftRotor.Position)}{(char)('A' + _middleRotor.Position)}{(char)('A' + _rightRotor.Position)}";
    }

    /// <summary>
    /// Creates a standard Enigma Model I machine with default settings.
    /// </summary>
    /// <param name="rotorOrder">Rotor order (e.g., "I II III").</param>
    /// <param name="rotorPositions">Initial rotor positions (e.g., "AAA").</param>
    /// <param name="ringSettings">Ring settings (e.g., "AAA" or "111").</param>
    /// <param name="reflectorType">Reflector type (default: UKW-B).</param>
    /// <param name="plugboardPairs">Plugboard pairs (e.g., "AB CD EF").</param>
    /// <returns>A configured Enigma Model I machine.</returns>
    public static EnigmaMachine CreateModelI(
        string rotorOrder = "I II III",
        string rotorPositions = "AAA",
        string ringSettings = "AAA",
        string reflectorType = "UKW-B",
        string plugboardPairs = "")
    {
        var rotors = rotorOrder.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (rotors.Length != 3)
            throw new ArgumentException("Rotor order must specify exactly 3 rotors.", nameof(rotorOrder));

        if (rotorPositions.Length != 3)
            throw new ArgumentException("Rotor positions must be exactly 3 characters.", nameof(rotorPositions));

        if (ringSettings.Length != 3)
            throw new ArgumentException("Ring settings must be exactly 3 characters.", nameof(ringSettings));

        // Convert positions and ring settings
        int[] positions = new int[3];
        int[] rings = new int[3];

        for (int i = 0; i < 3; i++)
        {
            positions[i] = char.ToUpper(rotorPositions[i]) - 'A';
            
            char ringSetting = char.ToUpper(ringSettings[i]);
            rings[i] = char.IsDigit(ringSetting) ? int.Parse(ringSetting.ToString()) - 1 : ringSetting - 'A';
        }

        // Create rotors
        Rotor leftRotor = CreateRotor(rotors[0], positions[0], rings[0]);
        Rotor middleRotor = CreateRotor(rotors[1], positions[1], rings[1]);
        Rotor rightRotor = CreateRotor(rotors[2], positions[2], rings[2]);

        // Create reflector
        Reflector reflector = reflectorType.ToUpper() switch
        {
            "UKW-A" or "A" => Reflector.ReflectorType.UKW_A(),
            "UKW-B" or "B" => Reflector.ReflectorType.UKW_B(),
            "UKW-C" or "C" => Reflector.ReflectorType.UKW_C(),
            _ => throw new ArgumentException($"Unknown reflector type: {reflectorType}", nameof(reflectorType))
        };

        // Create plugboard
        Plugboard plugboard = new(plugboardPairs);

        return new EnigmaMachine(leftRotor, middleRotor, rightRotor, reflector, plugboard);
    }

    private static Rotor CreateRotor(string type, int position, int ringSetting)
    {
        return type.ToUpper() switch
        {
            "I" or "1" => Rotor.RotorType.I(position, ringSetting),
            "II" or "2" => Rotor.RotorType.II(position, ringSetting),
            "III" or "3" => Rotor.RotorType.III(position, ringSetting),
            "IV" or "4" => Rotor.RotorType.IV(position, ringSetting),
            "V" or "5" => Rotor.RotorType.V(position, ringSetting),
            _ => throw new ArgumentException($"Unknown rotor type: {type}")
        };
    }
}
