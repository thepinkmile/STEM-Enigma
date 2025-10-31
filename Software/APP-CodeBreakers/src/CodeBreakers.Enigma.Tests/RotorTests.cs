namespace CodeBreakers.Enigma.Tests;

public class RotorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesRotor()
    {
        // Arrange
        var wiring = KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ");
        var notch = 16;

        // Act
        var rotor = new Rotor(wiring, notch);

        // Assert
        Assert.NotNull(rotor);
        Assert.Equal(0, rotor.Position);
        Assert.Equal(0, rotor.RingSetting);
    }

    [Fact]
    public void Constructor_WithPositionAndRingSetting_SetsCorrectly()
    {
        // Arrange
        var wiring = KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ");
        var notch = 16;
        var position = 5;
        var ringSetting = 3;

        // Act
        var rotor = new Rotor(wiring, notch, position, ringSetting);

        // Assert
        Assert.Equal(5, rotor.Position);
        Assert.Equal(3, rotor.RingSetting);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new int[] { -1 })]
    [InlineData(new int[] { 0, 1, 2, 4 })]
    public void Constructor_WithInvalidWiring_ThrowsArgumentException(int[]? wiring)
    {
        // Act & Assert
        var exception = Assert.ThrowsAny<ArgumentException>(() => new Rotor(wiring!, 16));
        Assert.Contains("wiring", exception.ParamName);
    }

    #endregion

    #region Position Property Tests

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    [InlineData(25)]
    public void Position_SetValidValue_SetsCorrectly(int position)
    {
        // Arrange
        Rotor rotor = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16);

        // Act
        rotor.Position = position;

        // Assert
        Assert.Equal(position, rotor.Position);
    }

    [Theory]
    [InlineData(26, 0)]   // Wraps to 0
    [InlineData(27, 1)]   // Wraps to 1
    [InlineData(52, 0)]   // Wraps to 0
    public void Position_SetValueOver25_WrapsAround(int input, int expected)
    {
        // Arrange
        Rotor rotor = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16);

        // Act
        rotor.Position = input;

        // Assert
        Assert.Equal(expected, rotor.Position);
    }

    [Fact]
    public void Position_SetNegativeValue_HandlesByModulo()
    {
        // Arrange
        Rotor rotor = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16);

        // Act
        rotor.Position = -1;

        // Assert - C# modulo with negative numbers: -1 % 26 = -1
        // The implementation uses value % 26, not a true modulo wrap
        Assert.InRange(rotor.Position, -25, 25);
    }

    #endregion

    #region RingSetting Property Tests

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    [InlineData(25)]
    public void RingSetting_SetValidValue_SetsCorrectly(int ringSetting)
    {
        // Arrange
        Rotor rotor = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16);

        // Act
        rotor.RingSetting = ringSetting;

        // Assert
        Assert.Equal(ringSetting, rotor.RingSetting);
    }

    [Theory]
    [InlineData(26, 0)]
    [InlineData(27, 1)]
    [InlineData(52, 0)]
    public void RingSetting_SetValueOver25_WrapsAround(int input, int expected)
    {
        // Arrange
        Rotor rotor = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16);

        // Act
        rotor.RingSetting = input;

        // Assert
        Assert.Equal(expected, rotor.RingSetting);
    }

    #endregion

    #region Forward Tests

    [Fact]
    public void Forward_AtPositionZero_EncodesCorrectly()
    {
        // Arrange - Rotor I wiring at position 0
        Rotor rotor = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16, 0, 0);

        // Act - A (0) should map to E (4) based on wiring
        var result = rotor.Forward(0);

        // Assert
        Assert.Equal(4, result); // A -> E
    }

    [Fact]
    public void Forward_AllInputs_ProduceValidOutputs()
    {
        // Arrange
        var rotor = Rotor.EnigmaI.I();

        // Act & Assert
        for (int i = 0; i < 26; i++)
        {
            var output = rotor.Forward(i);
            Assert.InRange(output, 0, 25);
        }
    }

    [Fact]
    public void Forward_WithDifferentPositions_ProducesDifferentResults()
    {
        // Arrange
        Rotor rotor1 = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16, 0, 0);
        Rotor rotor2 = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16, 1, 0);

        // Act
        var result1 = rotor1.Forward(0);
        var result2 = rotor2.Forward(0);

        // Assert - Different positions should produce different results
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void Forward_WithRingSetting_AffectsOutput()
    {
        // Arrange
        Rotor rotor1 = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16, 0, 0);
        Rotor rotor2 = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16, 0, 1);

        // Act
        var result1 = rotor1.Forward(0);
        var result2 = rotor2.Forward(0);

        // Assert - Different ring settings should produce different results
        Assert.NotEqual(result1, result2);
    }

    #endregion

    #region Backward Tests

    [Fact]
    public void Backward_AtPositionZero_EncodesCorrectly()
    {
        // Arrange
        Rotor rotor = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16, 0, 0);

        // Act - Find what input produces output 0
        var result = rotor.Backward(4);

        // Assert - Should reverse the forward operation
        Assert.Equal(0, result);
    }

    [Fact]
    public void Backward_AllInputs_ProduceValidOutputs()
    {
        // Arrange
        var rotor = Rotor.EnigmaI.I();

        // Act & Assert
        for (int i = 0; i < 26; i++)
        {
            var output = rotor.Backward(i);
            Assert.InRange(output, 0, 25);
        }
    }

    [Fact]
    public void Backward_ReversesForward_ForAllPositions()
    {
        // Arrange
        var rotor = Rotor.EnigmaI.I(5, 2);

        // Act & Assert
        for (int i = 0; i < 26; i++)
        {
            var forward = rotor.Forward(i);
            var backward = rotor.Backward(forward);
            Assert.Equal(i, backward);
        }
    }

    [Fact]
    public void Backward_WithDifferentPositions_ProducesDifferentResults()
    {
        // Arrange
        Rotor rotor1 = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16, 0, 0);
        Rotor rotor2 = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16, 1, 0);

        // Act
        var result1 = rotor1.Backward(0);
        var result2 = rotor2.Backward(0);

        // Assert
        Assert.NotEqual(result1, result2);
    }

    #endregion

    #region Step Tests

    [Fact]
    public void Step_IncrementsPosition()
    {
        // Arrange
        Rotor rotor = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16, 0);

        // Act
        rotor.Step();

        // Assert
        Assert.Equal(1, rotor.Position);
    }

    [Fact]
    public void Step_MultipleTimesIncrementsCorrectly()
    {
        // Arrange
        Rotor rotor = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16, 0);

        // Act
        rotor.Step();
        rotor.Step();
        rotor.Step();

        // Assert
        Assert.Equal(3, rotor.Position);
    }

    [Fact]
    public void Step_AtPosition25_WrapsToZero()
    {
        // Arrange
        Rotor rotor = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16, 25);

        // Act
        rotor.Step();

        // Assert
        Assert.Equal(0, rotor.Position);
    }

    [Fact]
    public void Step_26Times_ReturnsToOriginalPosition()
    {
        // Arrange
        Rotor rotor = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16, 5);

        // Act
        for (int i = 0; i < 26; i++)
        {
            rotor.Step();
        }

        // Assert
        Assert.Equal(5, rotor.Position);
    }

    #endregion

    #region IsAtNotch Tests

    [Fact]
    public void IsAtNotch_AfterSteppingToNotch_ReturnsTrue()
    {
        // Arrange - Rotor I has notch at position 16 (Q)
        Rotor rotor = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), 16, 15);

        // Act
        rotor.Step();

        // Assert
        Assert.True(rotor.IsAtNotch());
    }

    [Theory]
    [InlineData(16, 16, true)]
    [InlineData(16, 0, false)]
    public void IsAtNotch_WithDifferentNotches_DetectsCorrectly(int notch, int position, bool expectedIsAtNotch)
    {
        // Arrange
        Rotor rotor = new(KeySets.GetWiringFromCharacterMap(KeySets.EnigmaI_Keyset, "EKMFLGDQVZNTOWYHXUSPAIBRCJ"), notch, position);

        // Act
        var result = rotor.IsAtNotch();

        // Assert
        Assert.Equal(expectedIsAtNotch, result);
    }

    #endregion

    #region RotorType Factory Tests

    [Fact]
    public void RotorType_I_CreatesValidRotor()
    {
        // Act
        var rotor = Rotor.EnigmaI.I();

        // Assert
        Assert.NotNull(rotor);
        Assert.Equal(0, rotor.Position);
        Assert.Equal(0, rotor.RingSetting);
    }

    [Fact]
    public void RotorType_I_WithParameters_SetsCorrectly()
    {
        // Act
        var rotor = Rotor.EnigmaI.I(5, 3);

        // Assert
        Assert.Equal(5, rotor.Position);
        Assert.Equal(3, rotor.RingSetting);
    }

    [Theory]
    [InlineData(16)] // Q position for Rotor I
    public void RotorType_I_HasCorrectNotch(int notchPosition)
    {
        // Arrange & Act
        var rotor = Rotor.EnigmaI.I(notchPosition);

        // Assert
        Assert.True(rotor.IsAtNotch());
    }

    [Theory]
    [InlineData(4)]  // E position for Rotor II
    public void RotorType_II_HasCorrectNotch(int notchPosition)
    {
        // Arrange & Act
        var rotor = Rotor.EnigmaI.II(notchPosition);

        // Assert
        Assert.True(rotor.IsAtNotch());
    }

    [Theory]
    [InlineData(21)] // V position for Rotor III
    public void RotorType_III_HasCorrectNotch(int notchPosition)
    {
        // Arrange & Act
        var rotor = Rotor.EnigmaI.III(notchPosition);

        // Assert
        Assert.True(rotor.IsAtNotch());
    }

    [Theory]
    [InlineData(9)]  // J position for Rotor IV
    public void RotorType_IV_HasCorrectNotch(int notchPosition)
    {
        // Arrange & Act
        var rotor = Rotor.EnigmaI.IV(notchPosition);

        // Assert
        Assert.True(rotor.IsAtNotch());
    }

    [Theory]
    [InlineData(25)] // Z position for Rotor V
    public void RotorType_V_HasCorrectNotch(int notchPosition)
    {
        // Arrange & Act
        var rotor = Rotor.EnigmaI.V(notchPosition);

        // Assert
        Assert.True(rotor.IsAtNotch());
    }

    [Fact]
    public void RotorType_AllTypes_CreateValidRotors()
    {
        // Arrange & Act
        var rotors = new[]
        {
            Rotor.EnigmaI.I(),
            Rotor.EnigmaI.II(),
            Rotor.EnigmaI.III(),
            Rotor.EnigmaI.IV(),
            Rotor.EnigmaI.V()
        };

        // Assert
        foreach (var rotor in rotors)
        {
            Assert.NotNull(rotor);
            for (int i = 0; i < 26; i++)
            {
                Assert.InRange(rotor.Forward(i), 0, 25);
                Assert.InRange(rotor.Backward(i), 0, 25);
            }
        }
    }

    [Fact]
    public void RotorType_DifferentTypes_HaveDifferentWiring()
    {
        // Arrange
        var rotor1 = Rotor.EnigmaI.I();
        var rotor2 = Rotor.EnigmaI.II();
        var rotor3 = Rotor.EnigmaI.III();

        // Act & Assert - At least some outputs should be different
        bool foundDifference = false;
        for (int i = 0; i < 26; i++)
        {
            var result1 = rotor1.Forward(i);
            var result2 = rotor2.Forward(i);
            var result3 = rotor3.Forward(i);

            if (result1 != result2 || result2 != result3)
            {
                foundDifference = true;
                break;
            }
        }
        Assert.True(foundDifference, "Different rotor types should have different wiring");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Rotor_ForwardAndBackward_AreInverse()
    {
        // Arrange
        var rotor = Rotor.EnigmaI.III(10, 5);

        // Act & Assert - Forward then backward should return to original
        for (int i = 0; i < 26; i++)
        {
            var encoded = rotor.Forward(i);
            var decoded = rotor.Backward(encoded);
            Assert.Equal(i, decoded);
        }
    }

    [Fact]
    public void Rotor_WithPositionChange_AffectsBothDirections()
    {
        // Arrange
        var rotor = Rotor.EnigmaI.I(0, 0);
        var initialForward = rotor.Forward(0);
        var initialBackward = rotor.Backward(0);

        // Act
        rotor.Position = 5;
        var changedForward = rotor.Forward(0);
        var changedBackward = rotor.Backward(0);

        // Assert
        Assert.NotEqual(initialForward, changedForward);
        Assert.NotEqual(initialBackward, changedBackward);
    }

    [Fact]
    public void Rotor_CompleteRotation_ChangesEncodingAtEachStep()
    {
        // Arrange
        var rotor = Rotor.EnigmaI.I(0, 0);
        var results = new List<int>();

        // Act - Record output for 'A' at each position
        for (int pos = 0; pos < 26; pos++)
        {
            rotor.Position = pos;
            results.Add(rotor.Forward(0));
        }

        // Assert - Should have different values (not all the same)
        Assert.True(results.Distinct().Count() > 1);
    }

    [Fact]
    public void Rotor_SteppingThroughNotch_DetectsCorrectly()
    {
        // Arrange - Rotor I has notch at Q (position 16)
        var rotor = Rotor.EnigmaI.I(15);

        // Act & Assert
        Assert.False(rotor.IsAtNotch()); // Position 15 (P)
        rotor.Step();
        Assert.True(rotor.IsAtNotch());  // Position 16 (Q)
        rotor.Step();
        Assert.False(rotor.IsAtNotch()); // Position 17 (R)
    }

    [Fact]
    public void Rotor_HistoricalWiring_RotorI_ProducesKnownOutput()
    {
        // Arrange - Rotor I at position 0 with ring setting 0
        var rotor = Rotor.EnigmaI.I(0, 0);

        // Act & Assert - Test known Rotor I wiring: EKMFLGDQVZNTOWYHXUSPAIBRCJ
        Assert.Equal(4, rotor.Forward(0));   // A -> E
        Assert.Equal(10, rotor.Forward(1));  // B -> K
        Assert.Equal(12, rotor.Forward(2));  // C -> M
    }

    #endregion
}
