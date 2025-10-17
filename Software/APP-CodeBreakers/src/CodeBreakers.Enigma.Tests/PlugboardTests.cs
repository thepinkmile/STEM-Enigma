namespace CodeBreakers.Enigma.Tests;

public class PlugboardTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithEmptyString_CreatesPlugboardWithNoConnections()
    {
        // Arrange & Act
        var plugboard = new Plugboard("");

        // Assert
        Assert.NotNull(plugboard);
        Assert.Equal('A', plugboard.Swap('A')); // No swapping should occur
    }

    [Fact]
    public void Constructor_WithNull_CreatesPlugboardWithNoConnections()
    {
        // Arrange & Act
        var plugboard = new Plugboard(null!);

        // Assert
        Assert.NotNull(plugboard);
        Assert.Equal('Z', plugboard.Swap('Z')); // No swapping should occur
    }

    [Fact]
    public void Constructor_WithWhitespace_CreatesPlugboardWithNoConnections()
    {
        // Arrange & Act
        var plugboard = new Plugboard("   ");

        // Assert
        Assert.NotNull(plugboard);
        Assert.Equal('M', plugboard.Swap('M')); // No swapping should occur
    }

    [Fact]
    public void Constructor_WithValidPairs_CreatesPlugboardWithConnections()
    {
        // Arrange & Act
        var plugboard = new Plugboard("AB CD EF");

        // Assert
        Assert.NotNull(plugboard);
    }

    [Fact]
    public void Constructor_WithSinglePair_CreatesPlugboardWithOneConnection()
    {
        // Arrange & Act
        var plugboard = new Plugboard("AB");

        // Assert
        Assert.Equal('B', plugboard.Swap('A'));
        Assert.Equal('A', plugboard.Swap('B'));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("ABC")]
    [InlineData("ABCD")]
    public void Constructor_WithInvalidPairLength_ThrowsArgumentException(string invalidPair)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Plugboard(invalidPair));
        Assert.Contains("Invalid pair", exception.Message);
        Assert.Contains(invalidPair, exception.Message);
    }

    [Theory]
    [InlineData("AB AC")]  // A is used twice
    [InlineData("AB BA")]  // A and B are reused
    [InlineData("AB CD AE")]  // A is used again
    public void Constructor_WithDuplicateLetters_ThrowsArgumentException(string pairs)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Plugboard(pairs));
        Assert.Contains("Letter already connected", exception.Message);
    }

    [Fact]
    public void Constructor_WithLowercasePairs_CreatesValidConnections()
    {
        // Arrange & Act
        var plugboard = new Plugboard("ab cd");

        // Assert
        Assert.Equal('B', plugboard.Swap('A'));
        Assert.Equal('A', plugboard.Swap('B'));
        Assert.Equal('D', plugboard.Swap('C'));
        Assert.Equal('C', plugboard.Swap('D'));
    }

    [Fact]
    public void Constructor_WithMixedCasePairs_CreatesValidConnections()
    {
        // Arrange & Act
        var plugboard = new Plugboard("Ab cD Ef");

        // Assert
        Assert.Equal('B', plugboard.Swap('A'));
        Assert.Equal('D', plugboard.Swap('C'));
        Assert.Equal('F', plugboard.Swap('E'));
    }

    [Fact]
    public void Constructor_WithMaximumPairs_CreatesValidConnections()
    {
        // Arrange - Maximum 13 pairs (26 letters / 2)
        var plugboard = new Plugboard("AB CD EF GH IJ KL MN OP QR ST UV WX YZ");

        // Assert
        Assert.Equal('B', plugboard.Swap('A'));
        Assert.Equal('Z', plugboard.Swap('Y'));
    }

    [Fact]
    public void Constructor_WithExtraSpaces_CreatesValidConnections()
    {
        // Arrange & Act
        var plugboard = new Plugboard("AB   CD    EF");

        // Assert
        Assert.Equal('B', plugboard.Swap('A'));
        Assert.Equal('D', plugboard.Swap('C'));
        Assert.Equal('F', plugboard.Swap('E'));
    }

    #endregion

    #region Swap(char) Tests

    [Fact]
    public void SwapChar_WithConnectedLetter_ReturnsSwappedLetter()
    {
        // Arrange
        var plugboard = new Plugboard("AB CD EF");

        // Act & Assert
        Assert.Equal('B', plugboard.Swap('A'));
        Assert.Equal('A', plugboard.Swap('B'));
        Assert.Equal('D', plugboard.Swap('C'));
        Assert.Equal('C', plugboard.Swap('D'));
        Assert.Equal('F', plugboard.Swap('E'));
        Assert.Equal('E', plugboard.Swap('F'));
    }

    [Theory]
    [InlineData('G')]
    [InlineData('H')]
    [InlineData('Z')]
    public void SwapChar_WithUnconnectedLetter_ReturnsSameLetter(char letter)
    {
        // Arrange
        var plugboard = new Plugboard("AB CD EF");

        // Act
        var result = plugboard.Swap(letter);

        // Assert
        Assert.Equal(letter, result);
    }

    [Fact]
    public void SwapChar_WithLowercaseLetter_ConvertsToUppercaseAndSwaps()
    {
        // Arrange
        var plugboard = new Plugboard("AB CD");

        // Act & Assert
        Assert.Equal('B', plugboard.Swap('a'));
        Assert.Equal('A', plugboard.Swap('b'));
        Assert.Equal('D', plugboard.Swap('c'));
    }

    [Fact]
    public void SwapChar_IsSymmetric_SwappingTwiceReturnsOriginal()
    {
        // Arrange
        var plugboard = new Plugboard("AB CD EF");

        // Act & Assert
        Assert.Equal('A', plugboard.Swap(plugboard.Swap('A')));
        Assert.Equal('C', plugboard.Swap(plugboard.Swap('C')));
        Assert.Equal('G', plugboard.Swap(plugboard.Swap('G'))); // Unconnected
    }

    [Fact]
    public void SwapChar_WithNoConnections_ReturnsUnchangedLetters()
    {
        // Arrange
        var plugboard = new Plugboard();

        // Act & Assert
        for (char c = 'A'; c <= 'Z'; c++)
        {
            Assert.Equal(c, plugboard.Swap(c));
        }
    }

    #endregion

    #region Swap(int) Tests

    [Theory]
    [InlineData(0, 1)]   // A -> B
    [InlineData(1, 0)]   // B -> A
    [InlineData(2, 3)]   // C -> D
    [InlineData(3, 2)]   // D -> C
    public void SwapInt_WithConnectedPosition_ReturnsSwappedPosition(int input, int expected)
    {
        // Arrange
        var plugboard = new Plugboard("AB CD");

        // Act
        var result = plugboard.Swap(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(6)]   // G
    [InlineData(25)]  // Z
    public void SwapInt_WithUnconnectedPosition_ReturnsSamePosition(int position)
    {
        // Arrange
        var plugboard = new Plugboard("AB CD");

        // Act
        var result = plugboard.Swap(position);

        // Assert
        Assert.Equal(position, result);
    }

    [Fact]
    public void SwapInt_IsSymmetric_SwappingTwiceReturnsOriginal()
    {
        // Arrange
        var plugboard = new Plugboard("AB CD EF");

        // Act & Assert
        for (int i = 0; i < 26; i++)
        {
            Assert.Equal(i, plugboard.Swap(plugboard.Swap(i)));
        }
    }

    [Fact]
    public void SwapInt_WithNoConnections_ReturnsUnchangedPositions()
    {
        // Arrange
        var plugboard = new Plugboard();

        // Act & Assert
        for (int i = 0; i < 26; i++)
        {
            Assert.Equal(i, plugboard.Swap(i));
        }
    }

    [Fact]
    public void SwapInt_AllPositions_WorksCorrectly()
    {
        // Arrange
        var plugboard = new Plugboard("AZ BY CX DW");

        // Act & Assert
        Assert.Equal(25, plugboard.Swap(0));  // A(0) -> Z(25)
        Assert.Equal(0, plugboard.Swap(25));  // Z(25) -> A(0)
        Assert.Equal(24, plugboard.Swap(1));  // B(1) -> Y(24)
        Assert.Equal(1, plugboard.Swap(24));  // Y(24) -> B(1)
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Plugboard_CharAndIntSwap_ProduceConsistentResults()
    {
        // Arrange
        var plugboard = new Plugboard("AB CD EF GH IJ");

        // Act & Assert
        for (int i = 0; i < 26; i++)
        {
            char letter = (char)('A' + i);
            char swappedChar = plugboard.Swap(letter);
            int swappedInt = plugboard.Swap(i);

            // The char and int methods should produce consistent results
            Assert.Equal(swappedChar - 'A', swappedInt);
        }
    }

    [Fact]
    public void Plugboard_ComplexConfiguration_WorksCorrectly()
    {
        // Arrange - Historical Enigma plugboard setting
        var plugboard = new Plugboard("AD FT GW HY IJ KO LP NZ QM");

        // Act & Assert - Verify bidirectional swapping
        Assert.Equal('D', plugboard.Swap('A'));
        Assert.Equal('A', plugboard.Swap('D'));
        Assert.Equal('T', plugboard.Swap('F'));
        Assert.Equal('F', plugboard.Swap('T'));
        Assert.Equal('E', plugboard.Swap('E')); // Unconnected
    }

    [Fact]
    public void Plugboard_EmptyVsNullVsWhitespace_BehaveIdentically()
    {
        // Arrange
        var plugboard1 = new Plugboard("");
        var plugboard2 = new Plugboard(null!);
        var plugboard3 = new Plugboard("   ");

        // Act & Assert
        for (char c = 'A'; c <= 'Z'; c++)
        {
            Assert.Equal(plugboard1.Swap(c), plugboard2.Swap(c));
            Assert.Equal(plugboard2.Swap(c), plugboard3.Swap(c));
        }
    }

    #endregion
}
