namespace CodeBreakers.Enigma;

public static class KeySets
{
    public const string EnigmaI_Keyset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static int[] GetWiringFromCharacterMap(string keySet, string characterMapping)
        => [.. characterMapping.Select(x => keySet.IndexOf(x))];
}
