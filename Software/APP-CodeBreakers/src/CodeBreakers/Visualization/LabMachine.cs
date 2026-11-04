using CodeBreakers.Enigma;

namespace CodeBreakers.Visualization;

/// <summary>Validates lab settings and delegates every cipher operation to the existing shared engine.</summary>
public sealed class LabMachine
{
    private CodeBreakers.Enigma.EnigmaMachine _machine = CodeBreakers.Enigma.EnigmaMachine.CreateModelI();
    public CodeBreakers.Enigma.EnigmaMachine Engine => _machine;
    public static IReadOnlyList<string> Wirings { get; } = Array.AsReadOnly(
        CodeBreakers.Enigma.EnigmaMachine.CreateModelI().Rotors
            .Select(r => new string(r.Wiring.Select(p => (char)('A' + p)).ToArray())).ToArray());
    public string Positions => _machine.GetRotorPositions();
    public string Rings => new(_machine.Rotors.Select(r => (char)('A' + r.RingSetting)).ToArray());
    public IReadOnlyList<(int First, int Second)> PlugboardPairs { get; private set; } = Array.Empty<(int, int)>();

    public void Configure(string positions, string rings, string plugboard)
    {
        positions = ParseLetters(positions, nameof(positions));
        rings = ParseLetters(rings, nameof(rings));
        ArgumentNullException.ThrowIfNull(plugboard);
        var pairs = plugboard.ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (pairs.Length > 10) throw new ArgumentException("Use at most ten plugboard pairs.");
        var used = new HashSet<char>();
        var connections = new List<(int, int)>();
        foreach (var pair in pairs)
        {
            if (pair.Length != 2 || pair.Any(c => c is < 'A' or > 'Z') ||
                pair[0] == pair[1] || !used.Add(pair[0]) || !used.Add(pair[1]))
                throw new ArgumentException("Plugboard pairs must be distinct A-Z letters, e.g. AB CD.");
            // The existing Plugboard API stores directional connections, so supply both directions.
            connections.Add((pair[0] - 'A', pair[1] - 'A'));
            connections.Add((pair[1] - 'A', pair[0] - 'A'));
        }
        _machine = CodeBreakers.Enigma.EnigmaMachine.CreateModelI(
            rotorPositions: positions, ringSettings: rings, plugboardPairs: connections.ToArray());
        PlugboardPairs = Array.AsReadOnly(connections.Where((_, index) => index % 2 == 0).ToArray());
    }

    public KeyStroke Press(char letter)
    {
        letter = char.ToUpperInvariant(letter);
        if (letter is < 'A' or > 'Z') throw new ArgumentOutOfRangeException(nameof(letter), "Use A-Z only.");
        return _machine.EncodeCharWithTrace(letter);
    }

    private static string ParseLetters(string value, string name)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        value = value.ToUpperInvariant();
        if (value.Length != 3 || value.Any(c => c is < 'A' or > 'Z'))
            throw new ArgumentException("Positions and rings must each contain exactly three A-Z letters.", name);
        return value;
    }

    public static int Mod(int value) => (value % 26 + 26) % 26;
}
