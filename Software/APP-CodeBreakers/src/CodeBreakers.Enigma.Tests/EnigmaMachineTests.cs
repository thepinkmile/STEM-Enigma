namespace CodeBreakers.Enigma.Tests;

public class EnigmaMachineTests
{
    [Theory]
    [InlineData("hello world", "ILBDA AMTAZ")]
    [InlineData("enigma machine", "FQGAHW OXZNBML")]
    public void CreateModelIEncrypt_WithString_OutputsExpectedCypherText(string input, string expectedCypher)
    {
        EnigmaMachine enigma = EnigmaMachine.CreateModelI();
        string cypherText = enigma.Encrypt(input);
        Assert.Equal(expectedCypher, cypherText);
    }
}
