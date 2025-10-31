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
        Assert.Equal('A', plugboard.Remap('A')); // No swapping should occur
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

    #region Remap Tests

    [Fact]
    public void Remap_WithConnectedLetter_ReturnsSwappedLetter()
    {
        // Arrange
        var plugboard = new Plugboard(
            (0, 1), (1, 0),
            (2, 3), (3, 2),
            (4, 5), (5, 4)
        );

        // Act & Assert
        Assert.Equal(1, plugboard.Remap(0));
        Assert.Equal(0, plugboard.Remap(1));
        Assert.Equal(3, plugboard.Remap(2));
        Assert.Equal(2, plugboard.Remap(3));
        Assert.Equal(5, plugboard.Remap(4));
        Assert.Equal(4, plugboard.Remap(5));
    }

    [Theory]
    [InlineData('G')]
    [InlineData('H')]
    [InlineData('Z')]
    public void Remap_WithUnconnectedLetter_ReturnsSameLetter(char letter)
    {
        // Arrange
        var plugboard = new Plugboard((0, 1), (2, 3), (4, 5));

        // Act
        var result = plugboard.Remap(letter);

        // Assert
        Assert.Equal(letter, result);
    }

    [Fact]
    public void Remap_IsSymmetric_SwappingTwiceReturnsOriginal()
    {
        // Arrange
        var plugboard = new Plugboard(
            (0, 1), (1, 0),
            (2, 3), (3, 2),
            (4, 5), (5, 4)
        );

        // Act & Assert
        Assert.Equal(0, plugboard.Remap(plugboard.Remap(0)));
        Assert.Equal(2, plugboard.Remap(plugboard.Remap(2)));
        Assert.Equal(5, plugboard.Remap(plugboard.Remap(5))); // Unconnected
    }


    #endregion
}
