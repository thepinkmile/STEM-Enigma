namespace CodeBreakers.Visualization;

internal static class EnigmaKeyboard
{
    public static readonly string[] Rows = ["QWERTZUIO", "ASDFGHJK", "PYXCVBNML"];

    public static (int Row, int Column) Layout(int letter)
    {
        var index = string.Concat(Rows).IndexOf((char)('A' + letter));
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(letter));
        var row = index < 9 ? 0 : index < 17 ? 1 : 2;
        return (row, index - (row == 0 ? 0 : row == 1 ? 9 : 17));
    }

    public static float Depression(float progress)
    {
        static float Smooth(float value) => value * value * (3f - 2f * value);
        if (progress < 0f || progress >= 0.9f) return 0f;
        if (progress < 0.12f) return Smooth(progress / 0.12f);
        if (progress < 0.68f) return 1f;
        return 1f - Smooth((progress - 0.68f) / 0.22f);
    }

    // Single-stroke lettering lies on the key face instead of facing the camera.
    public static readonly string[] Glyphs =
    [
        "04 20 44|12 32", "00 04 34 43 32 02|32 41 30 00",
        "40 10 01 03 14 44", "00 04 24 43 41 20 00", "40 00 04 44|02 32",
        "04 00 40|02 32", "40 10 01 03 14 44 42 22", "00 04|40 44|02 42",
        "00 40|20 24|04 44", "00 30 41 44|24 44", "00 04|40 02 44",
        "00 04 44", "04 00 22 40 44", "04 00 44 40", "10 30 41 43 34 14 03 01 10",
        "04 00 30 41 32 02", "10 30 41 43 34 14 03 01 10|23 44",
        "04 00 30 41 32 02|22 44", "40 10 01 12 32 43 34 04",
        "00 40|20 24", "00 03 14 34 43 40", "00 24 40", "00 14 22 34 40",
        "00 44|40 04", "00 22 40|22 24", "00 40 04 44"
    ];
}
