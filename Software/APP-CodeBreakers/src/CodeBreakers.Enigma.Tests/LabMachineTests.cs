using Xunit;

namespace CodeBreakers.Enigma.Tests;

using EnigmaMachine = CodeBreakers.Visualization.LabMachine;

public sealed class LabMachineTests
{
    [Theory]
    [InlineData("AAAAA", "BDZGO")]
    [InlineData("HELLOWORLD", "ILBDAAMTAZ")]
    public void MatchesPublishedEnigmaIVectors(string plaintext, string ciphertext)
    {
        Assert.Equal(ciphertext, Encode(new EnigmaMachine(), plaintext));
    }

    [Theory]
    [InlineData("AAA", "AAA", "")]
    [InlineData("MCK", "BDF", "AV BS CG DL FU HZ IN KM OW RX")]
    [InlineData("ZZZ", "ZZZ", "AB CD EF")]
    public void SameSettingsDecryptAndNoLetterEncryptsToItself(string start, string rings, string plugs)
    {
        const string input = "THEQUICKBROWNFOXJUMPSOVERTHELAZYDOG";
        var machine = new EnigmaMachine();
        machine.Configure(start, rings, plugs);
        var encrypted = Encode(machine, input);
        Assert.All(input.Zip(encrypted), p => Assert.NotEqual(p.First, p.Second));
        machine.Configure(start, rings, plugs);
        Assert.Equal(input, Encode(machine, encrypted));
    }

    [Fact]
    public void ExposesOnePhysicalCablePerPairAndClearsOnReset()
    {
        var machine = new EnigmaMachine();
        machine.Configure("AAA", "AAA", "az by cx");
        Assert.Equal(new[] { (0, 25), (1, 24), (2, 23) }, machine.PlugboardPairs);
        Assert.Throws<ArgumentException>(() => machine.Configure("AAA", "AAA", "AZ AB"));
        Assert.Equal(3, machine.PlugboardPairs.Count);
        machine.Configure("AAA", "AAA", "");
        Assert.Empty(machine.PlugboardPairs);
    }

    [Fact]
    public void MiddleRotorDoubleStepsBeforeEncoding()
    {
        var machine = new EnigmaMachine();
        machine.Configure("ADU", "AAA", "");
        Assert.Equal("ADV", machine.Press('A').After);
        Assert.Equal("AEW", machine.Press('A').After);
        Assert.Equal("BFX", machine.Press('A').After);
    }

    [Fact]
    public void RingSettingDoesNotMoveWindowNotch()
    {
        var machine = new EnigmaMachine();
        machine.Configure("ADV", "BDF", "");
        Assert.Equal("AEW", machine.Press('A').After);
        Assert.Equal("BFX", machine.Press('A').After);
    }

    [Fact]
    public void WrapsAndAcceptsLowercase()
    {
        var machine = new EnigmaMachine();
        machine.Configure("ZZZ", "aaa", "ab cd");
        var stroke = machine.Press('a');
        Assert.Equal("ZZZ", stroke.Before);
        Assert.Equal("ZZA", stroke.After);
        Assert.Equal('A', stroke.Input);
    }

    [Fact]
    public void TraceIsContinuousAndLocalContactsMatchWiring()
    {
        var machine = new EnigmaMachine();
        machine.Configure("MCK", "BDF", "AB CD");
        foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            var stroke = machine.Press(letter);
            Assert.Equal(9, stroke.Hops.Count);
            Assert.Equal(letter - 'A', stroke.Hops[0].Input);
            Assert.Equal(stroke.Output - 'A', stroke.Hops[^1].Output);
            for (var i = 1; i < stroke.Hops.Count; i++)
                Assert.Equal(stroke.Hops[i - 1].Output, stroke.Hops[i].Input);
            foreach (var hop in stroke.Hops.Where(h => h.Rotor >= 0))
            {
                var wiring = EnigmaMachine.Wirings[hop.Rotor];
                Assert.Equal(hop.Returning ? hop.LocalInput : hop.LocalOutput,
                    wiring[hop.Returning ? hop.LocalOutput : hop.LocalInput] - 'A');
                var offset = stroke.After[hop.Rotor] - machine.Rings[hop.Rotor];
                Assert.Equal(EnigmaMachine.Mod(hop.Input + offset), hop.LocalInput);
                Assert.Equal(EnigmaMachine.Mod(hop.Output + offset), hop.LocalOutput);
            }
        }
    }

    [Theory]
    [InlineData("", "AAA", "")]
    [InlineData("AAAA", "AAA", "")]
    [InlineData("AA1", "AAA", "")]
    [InlineData("AAA", "A A", "")]
    [InlineData("AAA", "AAA", "AA")]
    [InlineData("AAA", "AAA", "AB AC")]
    [InlineData("AAA", "AAA", "ABC")]
    [InlineData("AAA", "AAA", "A1")]
    [InlineData("AAA", "AAA", "AB CD EF GH IJ KL MN OP QR ST UV")]
    public void RejectsInvalidConfigurationWithoutChangingState(string start, string rings, string plugs)
    {
        var machine = new EnigmaMachine();
        machine.Configure("MCK", "BDF", "AZ");
        Assert.Throws<ArgumentException>(() => machine.Configure(start, rings, plugs));
        Assert.Equal("MCK", machine.Positions);
        Assert.Equal("BDF", machine.Rings);
        var expected = new EnigmaMachine();
        expected.Configure("MCK", "BDF", "AZ");
        Assert.Equal(expected.Press('A').Output, machine.Press('A').Output);
    }

    [Theory]
    [InlineData('1')]
    [InlineData(' ')]
    [InlineData('!')]
    [InlineData('\u00E9')]
    public void InvalidKeyDoesNotStep(char key)
    {
        var machine = new EnigmaMachine();
        Assert.Throws<ArgumentOutOfRangeException>(() => machine.Press(key));
        Assert.Equal("AAA", machine.Positions);
    }

    private static string Encode(EnigmaMachine machine, string input) =>
        new(input.Select(c => machine.Press(c).Output).ToArray());
}
