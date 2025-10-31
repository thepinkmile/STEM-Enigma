namespace CodeBreakers.Enigma;

/// <summary>
/// Represents a reflector in the Enigma machine that reflects signals back through the rotors.
/// </summary>
public class Reflector
{
    private readonly int[] _wiring;

    public int WiringLength => _wiring.Length;

    /// <summary>
    /// Initializes a new reflector with the specified wiring.
    /// </summary>
    /// <param name="wiring">The wiring configuration (26 unique letters).</param>
    public Reflector(int[] wiring)
    {
        ArgumentNullException.ThrowIfNull(wiring);
        if (wiring.Any(x => x < 0 || x >= wiring.Length))
            throw new ArgumentException("Wiring values must be between 0 and the length of the wiring", nameof(wiring));

        _wiring = wiring;
    }

    /// <summary>
    /// Reflects the input signal.
    /// </summary>
    public int Reflect(int input)
    {
        return _wiring[input];
    }

    /// <summary>
    /// Historical Enigma I reflector configurations.
    /// </summary>
    public static class EnigmaI
    {
        public static Reflector UKW_A() => new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EJMZALYXVBWFCRQUONTSPIKHGD"));
        public static Reflector UKW_B() => new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "YRUHQSLDPXNGOKMIEBFZCWVJAT"));
        public static Reflector UKW_C() => new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "FVPJIAOYEDRZXWGCTKUQSBNMHL"));
    }
}
