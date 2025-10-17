namespace CodeBreakers.Enigma;

/// <summary>
/// Represents a reflector in the Enigma machine that reflects signals back through the rotors.
/// </summary>
public class Reflector
{
    private readonly string _wiring;

    /// <summary>
    /// Initializes a new reflector with the specified wiring.
    /// </summary>
    /// <param name="wiring">The wiring configuration (26 unique letters).</param>
    public Reflector(string wiring)
    {
        if (string.IsNullOrEmpty(wiring) || wiring.Length != 26)
            throw new ArgumentException("Wiring must contain exactly 26 characters.", nameof(wiring));

        _wiring = wiring.ToUpper();
    }

    /// <summary>
    /// Reflects the input signal.
    /// </summary>
    public int Reflect(int input)
    {
        return _wiring[input] - 'A';
    }

    /// <summary>
    /// Historical Enigma I reflector configurations.
    /// </summary>
    public static class ReflectorType
    {
        public static Reflector UKW_A() => new("EJMZALYXVBWFCRQUONTSPIKHGD");
        public static Reflector UKW_B() => new("YRUHQSLDPXNGOKMIEBFZCWVJAT");
        public static Reflector UKW_C() => new("FVPJIAOYEDRZXWGCTKUQSBNMHL");
    }
}
