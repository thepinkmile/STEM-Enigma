namespace CodeBreakers.Enigma;

/// <summary>
/// Represents the plugboard (Steckerbrett) of the Enigma machine.
/// </summary>
public class Plugboard
{
    internal static Plugboard Empty { get; } = new();

    private readonly Dictionary<int, int> _connections = [];

    internal int MaxPosition => 
        _connections.Keys.Concat(_connections.Values).DefaultIfEmpty(0).Max();

    /// <summary>
    /// Initializes a new plugboard with the specified letter pairs.
    /// </summary>
    /// <param name="pairs">Pairs of positions into the machine's key set</param>
    public Plugboard(params (int, int)[] connections)
    {
        foreach (var (source, destination) in connections)
        {
            if (source == destination)
                throw new ArgumentException($"Cannot connect position to itself: {source}");
            if (source < 0 || destination < 0)
                throw new ArgumentException("Positions must be non-negative.");
            if (_connections.ContainsKey(source))
                throw new ArgumentException($"Position already connected: {source}");

            _connections[source] = destination;
        }
    }

    /// <summary>
    /// Swaps the position if it's connected in the plugboard.
    /// </summary>
    public int Remap(int position)
    {
        return _connections.TryGetValue(position, out var swapped) ? swapped : position;
    }
}
