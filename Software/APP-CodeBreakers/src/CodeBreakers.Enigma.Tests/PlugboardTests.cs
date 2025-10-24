namespace CodeBreakers.Enigma.Tests;

public class PlugboardTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithEmptyString_CreatesPlugboardWithNoConnections()
    {
        // Arrange & Act
        var plugboard = new Plugboard();

        // Assert
        Assert.NotNull(plugboard);
        Assert.Equal('A', plugboard.Swap('A')); // No swapping should occur
    }

    [Fact]
    public void Constructor_WithValidPairs_CreatesPlugboardWithConnections()
    {
        // Arrange & Act
        var plugboard = new Plugboard((0, 1), (2, 3), (4, 5));

        // Assert
        Assert.NotNull(plugboard);
    }

    [Fact]
    public void Constructor_WithSourcePositions_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Plugboard((0, 1), (0, 2)));
        Assert.Contains("Position already connected", exception.Message);
    }

    #endregion

    #region Swap Tests

    [Fact]
    public void Swap_WithConnectedLetter_ReturnsSwappedLetter()
    {
        // Arrange
        var plugboard = new Plugboard(
            (0, 1), (1, 0),
            (2, 3), (3, 2),
            (4, 5), (5, 4)
        );

        // Act & Assert
        Assert.Equal(1, plugboard.Swap(0));
        Assert.Equal(0, plugboard.Swap(1));
        Assert.Equal(3, plugboard.Swap(2));
        Assert.Equal(2, plugboard.Swap(3));
        Assert.Equal(5, plugboard.Swap(4));
        Assert.Equal(4, plugboard.Swap(5));
    }

    [Theory]
    [InlineData('G')]
    [InlineData('H')]
    [InlineData('Z')]
    public void Swap_WithUnconnectedLetter_ReturnsSameLetter(char letter)
    {
        // Arrange
        var plugboard = new Plugboard((0, 1), (2, 3), (4, 5));

        // Act
        var result = plugboard.Swap(letter);

        // Assert
        Assert.Equal(letter, result);
    }

    [Fact]
    public void Swap_IsSymmetric_SwappingTwiceReturnsOriginal()
    {
        // Arrange
        var plugboard = new Plugboard(
            (0, 1), (1, 0),
            (2, 3), (3, 2),
            (4, 5), (5, 4)
        );

        // Act & Assert
        Assert.Equal(0, plugboard.Swap(plugboard.Swap(0)));
        Assert.Equal(2, plugboard.Swap(plugboard.Swap(2)));
        Assert.Equal(5, plugboard.Swap(plugboard.Swap(5))); // Unconnected
    }


    #endregion
}
