namespace CodeBreakers.Enigma.Tests;

public class ReflectorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidWiring_CreatesReflector()
    {
        // Arrange
        var wiring = "EJMZALYXVBWFCRQUONTSPIKHGD";

        // Act
        var reflector = new Reflector(wiring);

        // Assert
        Assert.NotNull(reflector);
    }

    [Fact]
    public void Constructor_WithLowercaseWiring_ConvertsToUppercase()
    {
        // Arrange
        var wiring = "ejmzalyxvbwfcrquontspikhgd";

        // Act
        var reflector = new Reflector(wiring);

        // Assert
        Assert.NotNull(reflector);
        // Verify it works correctly (E is at position 0, so input 0 should return 4 (E))
        Assert.Equal(4, reflector.Reflect(0));
    }

    [Fact]
    public void Constructor_WithMixedCaseWiring_ConvertsToUppercase()
    {
        // Arrange
        var wiring = "EjMzAlYxVbWfCrQuOnTsPiKhGd";

        // Act
        var reflector = new Reflector(wiring);

        // Assert
        Assert.NotNull(reflector);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_WithNullOrEmptyWiring_ThrowsArgumentException(string? wiring)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Reflector(wiring!));
        Assert.Contains("Wiring must contain exactly 26 characters", exception.Message);
    }

    [Theory]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXY")]   // 25 characters
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ1")] // 27 characters
    [InlineData("ABC")]                          // Too short
    public void Constructor_WithInvalidWiringLength_ThrowsArgumentException(string wiring)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Reflector(wiring));
        Assert.Contains("Wiring must contain exactly 26 characters", exception.Message);
    }

    #endregion

    #region Reflect Tests

    [Theory]
    [InlineData(0, 4)]   // A -> E (wiring: EJMZ...)
    [InlineData(1, 9)]   // B -> J
    [InlineData(2, 12)]  // C -> M
    public void Reflect_WithValidInput_ReturnsCorrectOutput(int input, int expected)
    {
        // Arrange - Using UKW-A wiring
        var reflector = new Reflector("EJMZALYXVBWFCRQUONTSPIKHGD");

        // Act
        var result = reflector.Reflect(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Reflect_AllPositions_ReturnsValidOutputs()
    {
        // Arrange
        var reflector = Reflector.ReflectorType.UKW_B();

        // Act & Assert
        for (int i = 0; i < 26; i++)
        {
            var output = reflector.Reflect(i);
            Assert.InRange(output, 0, 25);
        }
    }

    [Fact]
    public void Reflect_IsSymmetric_InputOutputArePaired()
    {
        // Arrange
        var reflector = Reflector.ReflectorType.UKW_B();

        // Act & Assert - If A reflects to E, then E must reflect to A
        for (int i = 0; i < 26; i++)
        {
            var output = reflector.Reflect(i);
            var reverseOutput = reflector.Reflect(output);
            Assert.Equal(i, reverseOutput);
        }
    }

    [Fact]
    public void Reflect_NeverReflectsToSamePosition_EnigmaProperty()
    {
        // Arrange - This is a fundamental Enigma property
        var reflector = Reflector.ReflectorType.UKW_B();

        // Act & Assert - A letter should never reflect to itself
        for (int i = 0; i < 26; i++)
        {
            var output = reflector.Reflect(i);
            Assert.NotEqual(i, output);
        }
    }

    [Fact]
    public void Reflect_WithCustomWiring_WorksCorrectly()
    {
        // Arrange - Create a simple test wiring where A->Z, B->Y, C->X, etc.
        var wiring = "ZYXWVUTSRQPONMLKJIHGFEDCBA";
        var reflector = new Reflector(wiring);

        // Act & Assert
        Assert.Equal(25, reflector.Reflect(0));  // A -> Z
        Assert.Equal(24, reflector.Reflect(1));  // B -> Y
        Assert.Equal(23, reflector.Reflect(2));  // C -> X
        Assert.Equal(0, reflector.Reflect(25));  // Z -> A
    }

    #endregion

    #region ReflectorType Factory Tests

    [Fact]
    public void ReflectorType_UKW_A_CreatesValidReflector()
    {
        // Act
        var reflector = Reflector.ReflectorType.UKW_A();

        // Assert
        Assert.NotNull(reflector);
        // Test a known reflection from UKW-A
        Assert.Equal(4, reflector.Reflect(0)); // A -> E
    }

    [Fact]
    public void ReflectorType_UKW_B_CreatesValidReflector()
    {
        // Act
        var reflector = Reflector.ReflectorType.UKW_B();

        // Assert
        Assert.NotNull(reflector);
        // Test a known reflection from UKW-B
        Assert.Equal(24, reflector.Reflect(0)); // A -> Y
    }

    [Fact]
    public void ReflectorType_UKW_C_CreatesValidReflector()
    {
        // Act
        var reflector = Reflector.ReflectorType.UKW_C();

        // Assert
        Assert.NotNull(reflector);
        // Test a known reflection from UKW-C
        Assert.Equal(5, reflector.Reflect(0)); // A -> F
    }

    [Fact]
    public void ReflectorType_AllTypes_AreSymmetric()
    {
        // Arrange
        var reflectors = new[]
        {
            Reflector.ReflectorType.UKW_A(),
            Reflector.ReflectorType.UKW_B(),
            Reflector.ReflectorType.UKW_C()
        };

        // Act & Assert
        foreach (var reflector in reflectors)
        {
            for (int i = 0; i < 26; i++)
            {
                var output = reflector.Reflect(i);
                var reverseOutput = reflector.Reflect(output);
                Assert.Equal(i, reverseOutput);
            }
        }
    }

    [Fact]
    public void ReflectorType_AllTypes_NeverReflectToSelf()
    {
        // Arrange
        var reflectors = new[]
        {
            Reflector.ReflectorType.UKW_A(),
            Reflector.ReflectorType.UKW_B(),
            Reflector.ReflectorType.UKW_C()
        };

        // Act & Assert
        foreach (var reflector in reflectors)
        {
            for (int i = 0; i < 26; i++)
            {
                var output = reflector.Reflect(i);
                Assert.NotEqual(i, output);
            }
        }
    }

    [Fact]
    public void ReflectorType_DifferentTypes_ProduceDifferentReflections()
    {
        // Arrange
        var ukwA = Reflector.ReflectorType.UKW_A();
        var ukwB = Reflector.ReflectorType.UKW_B();
        var ukwC = Reflector.ReflectorType.UKW_C();

        // Act & Assert - At least some reflections should be different
        bool foundDifference = false;
        for (int i = 0; i < 26; i++)
        {
            var resultA = ukwA.Reflect(i);
            var resultB = ukwB.Reflect(i);
            var resultC = ukwC.Reflect(i);

            if (resultA != resultB || resultB != resultC)
            {
                foundDifference = true;
                break;
            }
        }
        Assert.True(foundDifference, "All reflector types should produce different reflections");
    }

    #endregion

    #region Historical Accuracy Tests

    [Fact]
    public void UKW_A_HasCorrectHistoricalWiring()
    {
        // Arrange
        var reflector = Reflector.ReflectorType.UKW_A();
        var expectedWiring = new Dictionary<int, int>
        {
            { 0, 4 },   // A -> E
            { 4, 0 },   // E -> A
            { 1, 9 },   // B -> J
            { 9, 1 },   // J -> B
            { 2, 12 },  // C -> M
            { 12, 2 }   // M -> C
        };

        // Act & Assert
        foreach (var (input, expected) in expectedWiring)
        {
            Assert.Equal(expected, reflector.Reflect(input));
        }
    }

    [Fact]
    public void UKW_B_HasCorrectHistoricalWiring()
    {
        // Arrange
        var reflector = Reflector.ReflectorType.UKW_B();
        var expectedWiring = new Dictionary<int, int>
        {
            { 0, 24 },  // A -> Y
            { 24, 0 },  // Y -> A
            { 1, 17 },  // B -> R
            { 17, 1 },  // R -> B
            { 2, 20 },  // C -> U
            { 20, 2 }   // U -> C
        };

        // Act & Assert
        foreach (var (input, expected) in expectedWiring)
        {
            Assert.Equal(expected, reflector.Reflect(input));
        }
    }

    [Fact]
    public void UKW_C_HasCorrectHistoricalWiring()
    {
        // Arrange
        var reflector = Reflector.ReflectorType.UKW_C();
        var expectedWiring = new Dictionary<int, int>
        {
            { 0, 5 },   // A -> F
            { 5, 0 },   // F -> A
            { 1, 21 },  // B -> V
            { 21, 1 },  // V -> B
            { 2, 15 },  // C -> P
            { 15, 2 }   // P -> C
        };

        // Act & Assert
        foreach (var (input, expected) in expectedWiring)
        {
            Assert.Equal(expected, reflector.Reflect(input));
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Reflector_UsedInEnigmaMachine_MaintainsSymmetry()
    {
        // Arrange - Simulate Enigma machine reflector usage
        var reflector = Reflector.ReflectorType.UKW_B();
        var testData = Enumerable.Range(0, 26).ToArray();

        // Act - Reflect all positions twice
        var results = new List<int>();
        foreach (var input in testData)
        {
            var reflected = reflector.Reflect(input);
            var doubleReflected = reflector.Reflect(reflected);
            results.Add(doubleReflected);
        }

        // Assert - Double reflection should return to original
        Assert.Equal(testData, results);
    }

    [Fact]
    public void Reflector_AllPositionsMapped_NoDuplicates()
    {
        // Arrange
        var reflector = Reflector.ReflectorType.UKW_B();
        var outputs = new HashSet<int>();

        // Act
        for (int i = 0; i < 26; i++)
        {
            outputs.Add(reflector.Reflect(i));
        }

        // Assert - All 26 positions should be unique outputs
        Assert.Equal(26, outputs.Count);
    }

    #endregion
}
