namespace CodeBreakers.Enigma.Tests;

public class EnigmaMachineTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidComponents_CreatesEnigmaMachine()
    {
        // Arrange
        var leftRotor = Rotor.EnigmaI.I();
        var middleRotor = Rotor.EnigmaI.II();
        var rightRotor = Rotor.EnigmaI.III();
        var reflector = Reflector.EnigmaI.UKW_B();
        var plugboard = new Plugboard();

        // Act
        var enigma = new EnigmaMachine([leftRotor, middleRotor, rightRotor], reflector, plugboard);

        // Assert
        Assert.NotNull(enigma);
    }

    [Fact]
    public void Constructor_WithNullLeftRotor_ThrowsArgumentNullException()
    {
        // Arrange
        var middleRotor = Rotor.EnigmaI.II();
        var rightRotor = Rotor.EnigmaI.III();
        var reflector = Reflector.EnigmaI.UKW_B();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new EnigmaMachine([null!, middleRotor, rightRotor], reflector));
    }

    [Fact]
    public void Constructor_WithNullMiddleRotor_ThrowsArgumentNullException()
    {
        // Arrange
        var leftRotor = Rotor.EnigmaI.I();
        var rightRotor = Rotor.EnigmaI.III();
        var reflector = Reflector.EnigmaI.UKW_B();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new EnigmaMachine([leftRotor, null!, rightRotor], reflector));
    }

    [Fact]
    public void Constructor_WithNullRightRotor_ThrowsArgumentNullException()
    {
        // Arrange
        var leftRotor = Rotor.EnigmaI.I();
        var middleRotor = Rotor.EnigmaI.II();
        var reflector = Reflector.EnigmaI.UKW_B();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new EnigmaMachine([leftRotor, middleRotor, null!], reflector));
    }

    [Fact]
    public void Constructor_WithInvalidNumberOfRotors_ThrowsArgumentException()
    {
        // Arrange
        var reflector = Reflector.EnigmaI.UKW_B();
        var rotor1 = Rotor.EnigmaI.I();
        var rotor2 = Rotor.EnigmaI.II();
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new EnigmaMachine([rotor1, rotor2], reflector));
    }

    [Fact]
    public void Constructor_WithRotorWithDifferentWiringLength_ThrowsArgumentException()
    {
        // Arrange
        var leftRotor = new Rotor([0, 1, 2, 3, 4], notch: 0);
        var middleRotor = Rotor.EnigmaI.II();
        var rightRotor = Rotor.EnigmaI.III();
        var reflector = Reflector.EnigmaI.UKW_B();
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new EnigmaMachine([leftRotor, middleRotor, rightRotor], reflector));
    }

    [Fact]
    public void Constructor_WithNullReflector_ThrowsArgumentNullException()
    {
        // Arrange
        var leftRotor = Rotor.EnigmaI.I();
        var middleRotor = Rotor.EnigmaI.II();
        var rightRotor = Rotor.EnigmaI.III();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new EnigmaMachine([leftRotor, middleRotor, rightRotor], null!));
    }

    [Fact]
    public void Constructor_WithRelfectorWithDifferentWiringLength_ThrowsArgumentException()
    {
        // Arrange
        var leftRotor = Rotor.EnigmaI.I();
        var middleRotor = Rotor.EnigmaI.II();
        var rightRotor = Rotor.EnigmaI.III();
        var reflector = new Reflector([0, 1, 2]);
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new EnigmaMachine([leftRotor, middleRotor, rightRotor], reflector));
    }

    [Fact]
    public void Constructor_WithPlugboardWithOutOfRangeConnection_ThrowsArgumentException()
    {
        // Arrange
        var leftRotor = Rotor.EnigmaI.I();
        var middleRotor = Rotor.EnigmaI.II();
        var rightRotor = Rotor.EnigmaI.III();
        var reflector = Reflector.EnigmaI.UKW_B();

        // Act + Assert
        var ex = Assert.Throws<ArgumentException>(() => new EnigmaMachine([leftRotor, middleRotor, rightRotor], reflector, new Plugboard((0, 26))));
        Assert.Equal("plugboard", ex.ParamName);
    }

    #endregion

    #region EncodeChar Tests

    [Theory]
    [InlineData('A')]
    [InlineData('M')]
    [InlineData('Z')]
    public void EncodeChar_WithValidLetter_ReturnsEncodedLetter(char input)
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI();

        // Act
        var result = enigma.EncodeChar(input);

        // Assert
        Assert.True(result >= 'A' && result <= 'Z');
        Assert.NotEqual(input, result); // Enigma never encode a letter to itself
    }

    [Theory]
    [InlineData('a')]
    [InlineData('z')]
    public void EncodeChar_WithLowercaseLetter_ConvertsToUppercaseAndEncoded(char input)
    {
        // Arrange
        var enigma1 = EnigmaMachine.CreateModelI();
        var enigma2 = EnigmaMachine.CreateModelI();

        // Act
        var resultLowercase = enigma1.EncodeChar(input);
        var resultUppercase = enigma2.EncodeChar(char.ToUpper(input));

        // Assert
        Assert.Equal(resultLowercase, resultUppercase);
        Assert.True(resultLowercase >= 'A' && resultLowercase <= 'Z');
    }

    [Fact]
    public void EncodeChar_RotorsAdvanceBeforeEncodeion()
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI(rotorPositions: "AAA");
        var initialPositions = enigma.GetRotorPositions();

        // Act
        enigma.EncodeChar('A');
        var newPositions = enigma.GetRotorPositions();

        // Assert
        Assert.NotEqual(initialPositions, newPositions);
        Assert.Equal("AAB", newPositions);
    }

    #endregion

    #region Encode Tests

    [Theory]
    [InlineData("HELLOWORLD", "ILBDAAMTAZ")]
    [InlineData("ENIGMAMACHINE", "FQGAHWOXZNBML")]
    public void Encode_WithString_OutputsExpectedCypherText(string input, string expectedCypher)
    {
        // Arrange
        EnigmaMachine enigma = EnigmaMachine.CreateModelI();
        
        // Act
        string cypherText = enigma.Encode(input);
        
        // Assert
        Assert.Equal(expectedCypher, cypherText);
    }

    [Fact]
    public void Encode_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI();

        // Act
        var result = enigma.Encode("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Encode_WithNull_ReturnsNull()
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI();

        // Act
        var result = enigma.Encode(null!);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("AAAAA")]
    [InlineData("HELLO")]
    public void Encode_IsReversible_DecryptsCiphertext(string plaintext)
    {
        // Arrange
        var enigma1 = EnigmaMachine.CreateModelI(rotorPositions: "ABC");
        var enigma2 = EnigmaMachine.CreateModelI(rotorPositions: "ABC");

        // Act
        var ciphertext = enigma1.Encode(plaintext);
        var decrypted = enigma2.Encode(ciphertext);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encode_WithMixedCase_ConvertsToUppercase()
    {
        // Arrange
        var enigma1 = EnigmaMachine.CreateModelI();
        var enigma2 = EnigmaMachine.CreateModelI();

        // Act
        var result1 = enigma1.Encode("HeLLo");
        var result2 = enigma2.Encode("HELLO");

        // Assert
        Assert.Equal(result2, result1);
    }

    [Fact]
    public void Encode_WithInvalidCharacter_ThrowsArgumentException()
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => enigma.Encode("HELLO WORLD!"));
    }

    #endregion

    #region Rotor Stepping Tests

    [Fact]
    public void EncodeChar_RightRotorStepsEveryTime()
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI(rotorPositions: "AAA");

        // Act
        enigma.EncodeChar('A');
        enigma.EncodeChar('A');
        enigma.EncodeChar('A');

        // Assert
        Assert.Equal("AAD", enigma.GetRotorPositions());
    }

    [Fact]
    public void EncodeChar_MiddleRotorStepsAtNotch()
    {
        // Arrange
        // Rotor III has notch at V (position 21)
        // When right rotor is at the notch, it triggers middle rotor to step on the next encode
        var enigma = EnigmaMachine.CreateModelI(rotorPositions: "AAV");

        // Act
        enigma.EncodeChar('A'); // Right rotor at notch

        // Assert - right rotor always steps, and when at notch it also steps middle rotor
        var positions = enigma.GetRotorPositions();
        Assert.StartsWith("AB", positions); // Middle rotor stepped
        Assert.Equal('W', positions[2]); // Right rotor stepped
    }

    [Fact]
    public void EncodeChar_DoubleSteppingOccurs()
    {
        // Arrange
        // Rotor II has notch at E (position 4)
        // Double-stepping: when middle rotor is at notch, both middle and left step
        var enigma = EnigmaMachine.CreateModelI(rotorOrder: "I II III", rotorPositions: "AEV");

        // Act
        enigma.EncodeChar('A'); // Middle rotor at notch triggers double-stepping

        // Assert - middle at notch causes both left and middle to step, plus right always steps
        var positions = enigma.GetRotorPositions();
        Assert.Equal('B', positions[0]); // Left stepped
        Assert.Equal('F', positions[1]); // Middle stepped
        Assert.Equal('W', positions[2]); // Right stepped
    }

    #endregion

    #region Rotor Position Tests

    [Theory]
    [InlineData(0, 0, 0, "AAA")]
    [InlineData(5, 10, 15, "FKP")]
    [InlineData(25, 25, 25, "ZZZ")]
    public void SetRotorPositions_SetsCorrectPositions(int left, int middle, int right, string expected)
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI();

        // Act
        enigma.SetRotorPositions(left, middle, right);

        // Assert
        Assert.Equal(expected, enigma.GetRotorPositions());
    }

    [Fact]
    public void GetRotorPositions_ReturnsCorrectFormat()
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI(rotorPositions: "XYZ");

        // Act
        var positions = enigma.GetRotorPositions();

        // Assert
        Assert.Equal("XYZ", positions);
        Assert.Equal(3, positions.Length);
    }

    #endregion

    #region CreateModelI Tests

    [Fact]
    public void CreateModelI_WithDefaultParameters_CreatesValidMachine()
    {
        // Act
        var enigma = EnigmaMachine.CreateModelI();

        // Assert
        Assert.NotNull(enigma);
        Assert.Equal("AAA", enigma.GetRotorPositions());
    }

    [Theory]
    [InlineData("I II III")]
    [InlineData("III II I")]
    [InlineData("V IV III")]
    public void CreateModelI_WithDifferentRotorOrders_CreatesValidMachine(string rotorOrder)
    {
        // Act
        var enigma = EnigmaMachine.CreateModelI(rotorOrder: rotorOrder);

        // Assert
        Assert.NotNull(enigma);
    }

    [Theory]
    [InlineData("I II")]
    [InlineData("I II III IV")]
    [InlineData("")]
    public void CreateModelI_WithInvalidRotorCount_ThrowsArgumentException(string rotorOrder)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            EnigmaMachine.CreateModelI(rotorOrder: rotorOrder));
    }

    [Theory]
    [InlineData("AA")]
    [InlineData("AAAA")]
    [InlineData("")]
    public void CreateModelI_WithInvalidRotorPositionsLength_ThrowsArgumentException(string positions)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            EnigmaMachine.CreateModelI(rotorPositions: positions));
    }

    [Theory]
    [InlineData("AA")]
    [InlineData("AAAA")]
    [InlineData("")]
    public void CreateModelI_WithInvalidRingSettingsLength_ThrowsArgumentException(string ringSettings)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            EnigmaMachine.CreateModelI(ringSettings: ringSettings));
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("XYZ")]
    [InlineData("aaa")]
    public void CreateModelI_WithValidRotorPositions_SetsPositionsCorrectly(string positions)
    {
        // Act
        var enigma = EnigmaMachine.CreateModelI(rotorPositions: positions);

        // Assert
        Assert.Equal(positions.ToUpper(), enigma.GetRotorPositions());
    }

    [Theory]
    [InlineData("UKW-A")]
    [InlineData("UKW-B")]
    [InlineData("UKW-C")]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("C")]
    public void CreateModelI_WithDifferentReflectors_CreatesValidMachine(string reflectorType)
    {
        // Act
        var enigma = EnigmaMachine.CreateModelI(reflectorType: reflectorType);

        // Assert
        Assert.NotNull(enigma);
    }

    [Theory]
    [InlineData("UKW-D")]
    [InlineData("INVALID")]
    [InlineData("X")]
    public void CreateModelI_WithInvalidReflector_ThrowsArgumentException(string reflectorType)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            EnigmaMachine.CreateModelI(reflectorType: reflectorType));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    [InlineData("5")]
    public void CreateModelI_WithNumericRotorTypes_CreatesValidMachine(string rotorType)
    {
        // Act
        var enigma = EnigmaMachine.CreateModelI(rotorOrder: $"{rotorType} II III");

        // Assert
        Assert.NotNull(enigma);
    }

    [Theory]
    [InlineData("VI II III")]
    [InlineData("X Y Z")]
    public void CreateModelI_WithInvalidRotorType_ThrowsArgumentException(string rotorOrder)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            EnigmaMachine.CreateModelI(rotorOrder: rotorOrder));
    }

    #endregion

    #region Historical Accuracy Tests

    [Fact]
    public void Encode_WithKnownHistoricalSettings_ProducesCorrectOutput()
    {
        // Arrange - Using a known Enigma configuration
        var enigma = EnigmaMachine.CreateModelI(
            rotorOrder: "I II III",
            rotorPositions: "AAA",
            ringSettings: "AAA",
            reflectorType: "UKW-B"
        );

        // Act
        var result = enigma.Encode("AAAAA");

        // Assert - First character should never be 'A' (Enigma never encodes to itself)
        Assert.NotEqual('A', result[0]);
    }

    [Fact]
    public void Encode_EnigmaPropertyNeverEncodedLetterToItself()
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI();
        var plaintext = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        // Act
        var ciphertext = enigma.Encode(plaintext);

        // Assert
        for (int i = 0; i < plaintext.Length; i++)
        {
            Assert.NotEqual(plaintext[i], ciphertext[i]);
        }
    }

    [Fact]
    public void Encode_WithIdenticalSettings_ProducesIdenticalOutput()
    {
        // Arrange
        var enigma1 = EnigmaMachine.CreateModelI(
            rotorOrder: "III I II",
            rotorPositions: "MCK",
            ringSettings: "ABC",
            reflectorType: "UKW-B",
            plugboardPairs: [(0, 1), (2, 3)]
        );

        var enigma2 = EnigmaMachine.CreateModelI(
            rotorOrder: "III I II",
            rotorPositions: "MCK",
            ringSettings: "ABC",
            reflectorType: "UKW-B",
            plugboardPairs: [(0, 1), (2, 3)]
        );

        var plaintext = "HELLO";

        // Act
        var result1 = enigma1.Encode(plaintext);
        var result2 = enigma2.Encode(plaintext);

        // Assert
        Assert.Equal(result1, result2);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CompleteEncodeionDecryption_WithComplexSettings_WorksCorrectly()
    {
        // Arrange
        var settings = (
            rotorOrder: "V III I",
            rotorPositions: "WXC",
            ringSettings: "ABC",
            reflectorType: "UKW-C",
            plugboardPairs: new(int, int)[]
            { 
                (0, 3), (3, 0),
                (5, 19), (19, 5),
                (6, 22), (22, 6),
                (7, 24), (24, 7),
                (8, 9), (9, 8),
                (10, 14), (14, 10),
                (11, 15), (15, 11),
                (13, 25), (25, 13),
                (16, 12), (12, 16)
            }
        );

        var enigma1 = EnigmaMachine.CreateModelI(
            settings.rotorOrder, 
            settings.rotorPositions, 
            settings.ringSettings, 
            settings.reflectorType, 
            settings.plugboardPairs
        );

        var enigma2 = EnigmaMachine.CreateModelI(
            settings.rotorOrder, 
            settings.rotorPositions, 
            settings.ringSettings, 
            settings.reflectorType, 
            settings.plugboardPairs
        );

        var plaintext = "THEQUICKBROWNFOXJUMPSOVERTHELAZYDOG";

        // Act
        var ciphertext = enigma1.Encode(plaintext);
        var decrypted = enigma2.Encode(ciphertext);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encode_LongMessage_MaintainsSymmetricProperty()
    {
        // Arrange
        var longMessage = string.Concat(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 10));
        var enigma1 = EnigmaMachine.CreateModelI(rotorPositions: "AAA");
        var enigma2 = EnigmaMachine.CreateModelI(rotorPositions: "AAA");

        // Act
        var encoded = enigma1.Encode(longMessage);
        var decoded = enigma2.Encode(encoded);

        // Assert
        Assert.Equal(longMessage, decoded);
        Assert.Equal(longMessage.Length, encoded.Length);
    }

    #endregion
}
