namespace CodeBreakers.Enigma;

/// <summary>
/// Represents the plugboard (Steckerbrett) of the Enigma machine.
/// </summary>
public class Plugboard
{
    private readonly Dictionary<char, char> _connections = [];

    /// <summary>
    /// Initializes a new plugboard with the specified letter pairs.
    /// </summary>
    /// <param name="pairs">Space-separated pairs of letters to swap (e.g., "AB CD EF").</param>
    public Plugboard(string pairs = "")
    {
        if (string.IsNullOrWhiteSpace(pairs))
            return;

        var pairList = pairs.ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairList)
        {
            if (pair.Length != 2)
                throw new ArgumentException($"Invalid pair: {pair}. Each pair must contain exactly 2 letters.");

            char a = pair[0];
            char b = pair[1];

            if (_connections.ContainsKey(a) || _connections.ContainsKey(b))
                throw new ArgumentException($"Letter already connected: {pair}");

            _connections[a] = b;
            _connections[b] = a;
        }
    }

    /// <summary>
    /// Swaps the letter if it's connected in the plugboard.
    /// </summary>
    public char Swap(char letter)
    {
        letter = char.ToUpper(letter);
        return _connections.TryGetValue(letter, out var swapped) ? swapped : letter;
    }

    /// <summary>
    /// Swaps the letter by position (0-25).
    /// </summary>
    public int Swap(int position)
    {
        char letter = (char)('A' + position);
        char swapped = Swap(letter);
        return swapped - 'A';
    }
}
