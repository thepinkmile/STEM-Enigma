using CodeBreakers.Visualization;

namespace CodeBreakers.Enigma.Tests;

public sealed class EnigmaKeyboardTests
{
    [Fact]
    public void HistoricalRowsContainEachLetterExactlyOnce()
    {
        Assert.Equal(new[] { "QWERTZUIO", "ASDFGHJK", "PYXCVBNML" }, EnigmaKeyboard.Rows);
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWXYZ", new string(string.Concat(EnigmaKeyboard.Rows).Order().ToArray()));
        for (var letter = 0; letter < 26; letter++)
        {
            var (row, column) = EnigmaKeyboard.Layout(letter);
            Assert.Equal((char)('A' + letter), EnigmaKeyboard.Rows[row][column]);
        }
    }

    [Fact]
    public void KeyTravelPressesHoldsAndReleasesWithoutOvershooting()
    {
        Assert.Equal(0f, EnigmaKeyboard.Depression(0f));
        Assert.Equal(0.5f, EnigmaKeyboard.Depression(0.06f), 5);
        Assert.Equal(1f, EnigmaKeyboard.Depression(0.12f));
        Assert.Equal(1f, EnigmaKeyboard.Depression(0.68f));
        Assert.Equal(0.5f, EnigmaKeyboard.Depression(0.79f), 5);
        Assert.Equal(0f, EnigmaKeyboard.Depression(0.9f));
        Assert.Equal(0f, EnigmaKeyboard.Depression(1f));
        Assert.Equal(0f, EnigmaKeyboard.Depression(-1f));
        for (var i = 0; i <= 100; i++)
            Assert.InRange(EnigmaKeyboard.Depression(i / 100f), 0f, 1f);
    }

    [Fact]
    public void EveryKeyHasAValidFaceGlyph()
    {
        Assert.Equal(26, EnigmaKeyboard.Glyphs.Length);
        foreach (var glyph in EnigmaKeyboard.Glyphs)
            foreach (var stroke in glyph.Split('|'))
            {
                var points = stroke.Split(' ');
                Assert.True(points.Length >= 2);
                foreach (var point in points)
                {
                    Assert.Equal(2, point.Length);
                    Assert.All(point, digit => Assert.InRange(digit, '0', '4'));
                }
            }
    }
}
