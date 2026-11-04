namespace CodeBreakers.Enigma;

public sealed record SignalHop(string Stage, int Input, int Output, int Rotor = -1,
    int LocalInput = -1, int LocalOutput = -1, bool Returning = false);

public sealed record KeyStroke(char Input, char Output, string Before, string After,
    IReadOnlyList<SignalHop> Hops);
