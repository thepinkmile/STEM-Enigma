namespace CodeBreakers.Enigma.Tests;

public sealed class TraceTests
{
    [Fact]
    public void TracedAndOrdinaryEncodingShareTheSameEngineAndStepping()
    {
        var plain = EnigmaMachine.CreateModelI(rotorPositions: "ADU", ringSettings: "BDF");
        var traced = EnigmaMachine.CreateModelI(rotorPositions: "ADU", ringSettings: "BDF");
        foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            var before = traced.GetRotorPositions();
            var stroke = traced.EncodeCharWithTrace(letter);
            Assert.Equal(plain.EncodeChar(letter), stroke.Output);
            Assert.Equal(before, stroke.Before);
            Assert.Equal(plain.GetRotorPositions(), stroke.After);
            Assert.Equal(9, stroke.Hops.Count);
        }
    }

    [Fact]
    public void ExistingEngineDoubleStepsTheMiddleRotor()
    {
        var machine = EnigmaMachine.CreateModelI(rotorPositions: "ADU");
        machine.EncodeChar('A');
        Assert.Equal("ADV", machine.GetRotorPositions());
        machine.EncodeChar('A');
        Assert.Equal("AEW", machine.GetRotorPositions());
        machine.EncodeChar('A');
        Assert.Equal("BFX", machine.GetRotorPositions());
    }
}
