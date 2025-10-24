using System.Text;

namespace CodeBreakers.Enigma;

/// <summary>
/// Represents an Enigma Model I machine with three rotors, a reflector, and a plugboard.
/// </summary>
public class EnigmaMachine
{
    public IReadOnlyList<Rotor> Rotors { get; }
    private char[] CharacterSet { get; } = KeySets.EnigmaI_Keyset.ToCharArray();

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
        ICollection<Rotor> rotors,
        Reflector reflector,
        Plugboard? plugboard = null)
    {
        ArgumentNullException.ThrowIfNull(rotors);
        if (rotors.Count < 3) throw new ArgumentOutOfRangeException(nameof(rotors), "At least 3 rotors are required.");
        if (rotors.Any(x => x is null)) throw new ArgumentException("Rotor collection contains null elements.", nameof(rotors));
        if (rotors.Any(x => x.WiringLength != CharacterSet.Length)) throw new ArgumentException("All rotors must have wiring length equal to character set length.", nameof(rotors));
        Rotors = [..rotors];

        ArgumentNullException.ThrowIfNull(reflector);
        if (reflector.WiringLength != CharacterSet.Length)
            throw new ArgumentException("Reflector wiring length must equal character set length.", nameof(reflector));
        _reflector = reflector;

        _plugboard = plugboard ?? new Plugboard();
    }

    /// <summary>
    /// Encodes or decodes a single character.
    /// </summary>
    /// <param name="input">The character to encode/decode.</param>
    /// <returns>The encoded/decoded character.</returns>
    public char EncodeChar(char input)
    {
        input = char.ToUpper(input);
        int position = CharacterSet.IndexOf(input);
        if (position == -1)
            throw new ArgumentException($"Input character '{input}' is not in the valid character set [{string.Join("", CharacterSet)}]");

        // Step rotors before encoding (double-stepping mechanism)
        StepRotors();

        // Through plugboard
        position = _plugboard.Swap(position);

        // Forward through rotors (right to left)
        for (int i = Rotors.Count - 1; i >= 0; i--)
        {
            position = Rotors[i].Forward(position);
        }

        // Through reflector
        position = _reflector.Reflect(position);

        // Backward through rotors (left to right)
        for (int i = 0; i < Rotors.Count; i++)
        {
            position = Rotors[i].Backward(position);
        }

        // Through plugboard again
        position = _plugboard.Swap(position);

        // Convert back to letter
        return CharacterSet[position];
    }

    /// <summary>
    /// Encodes or decodes a text string.
    /// </summary>
    /// <param name="text">The text to encode/decode.</param>
    /// <returns>The encoded/decoded text.</returns>
    public string Encode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new char[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            result[i] = EncodeChar(text[i]);
        }

        return new string(result);
    }

    /// <summary>
    /// Steps the rotors according to the double-stepping mechanism.
    /// </summary>
    private void StepRotors()
    {
        for(int i = 0; i < Rotors.Count; i++)
        {
            if (i == Rotors.Count - 1 || Rotors[i + 1].IsAtNotch())
            {
                Rotors[i].Step();
            }
        }
    }

    /// <summary>
    /// Resets the rotor positions to the specified settings.
    /// </summary>
    /// <param name="leftPosition">Left rotor position (0-25 or A-Z).</param>
    /// <param name="middlePosition">Middle rotor position (0-25 or A-Z).</param>
    /// <param name="rightPosition">Right rotor position (0-25 or A-Z).</param>
    [Obsolete("This API needs to be redone to work with a list")]
    public void SetRotorPositions(int leftPosition, int middlePosition, int rightPosition)
    {
        Rotors[0].Position = leftPosition;
        Rotors[1].Position = middlePosition;
        Rotors[2].Position = rightPosition;
    }

    /// <summary>
    /// Gets the current rotor positions as a string (e.g., "ABC").
    /// </summary>
    public string GetRotorPositions()
    {
        StringBuilder sb = new();

        foreach (var rotor in Rotors)
        {
            sb.Append(CharacterSet[rotor.Position]);
        }

        return sb.ToString();
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
            "UKW-A" or "A" => Reflector.EnigmaI.UKW_A(),
            "UKW-B" or "B" => Reflector.EnigmaI.UKW_B(),
            "UKW-C" or "C" => Reflector.EnigmaI.UKW_C(),
            _ => throw new ArgumentException($"Unknown reflector type: {reflectorType}", nameof(reflectorType))
        };

        // Create plugboard
        Plugboard plugboard = new(plugboardPairs);

        return new EnigmaMachine([leftRotor, middleRotor, rightRotor], reflector, plugboard);
    }

    private static Rotor CreateRotor(string type, int position, int ringSetting)
    {
        return type.ToUpper() switch
        {
            "I" or "1" => Rotor.EnigmaI.I(position, ringSetting),
            "II" or "2" => Rotor.EnigmaI.II(position, ringSetting),
            "III" or "3" => Rotor.EnigmaI.III(position, ringSetting),
            "IV" or "4" => Rotor.EnigmaI.IV(position, ringSetting),
            "V" or "5" => Rotor.EnigmaI.V(position, ringSetting),
            _ => throw new ArgumentException($"Unknown rotor type: {type}")
        };
    }
}
