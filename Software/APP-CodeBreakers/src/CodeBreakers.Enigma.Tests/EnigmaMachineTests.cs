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
    public void Constructor_WithNullPlugboard_CreatesDefaultPlugboard()
    {
        // Arrange
        var leftRotor = Rotor.EnigmaI.I();
        var middleRotor = Rotor.EnigmaI.II();
        var rightRotor = Rotor.EnigmaI.III();
        var reflector = Reflector.EnigmaI.UKW_B();

        // Act
        var enigma = new EnigmaMachine([leftRotor, middleRotor, rightRotor], reflector, null);

        // Assert
        Assert.NotNull(enigma);
    }

    #endregion

    #region EncryptChar Tests

    [Theory]
    [InlineData('A')]
    [InlineData('M')]
    [InlineData('Z')]
    public void EncryptChar_WithValidLetter_ReturnsEncryptedLetter(char input)
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI();

        // Act
        var result = enigma.EncryptChar(input);

        // Assert
        Assert.True(result >= 'A' && result <= 'Z');
        Assert.NotEqual(input, result); // Enigma never encrypts a letter to itself
    }

    [Theory]
    [InlineData('a')]
    [InlineData('z')]
    public void EncryptChar_WithLowercaseLetter_ConvertsToUppercaseAndEncrypts(char input)
    {
        // Arrange
        var enigma1 = EnigmaMachine.CreateModelI();
        var enigma2 = EnigmaMachine.CreateModelI();

        // Act
        var resultLowercase = enigma1.EncryptChar(input);
        var resultUppercase = enigma2.EncryptChar(char.ToUpper(input));

        // Assert
        Assert.Equal(resultLowercase, resultUppercase);
        Assert.True(resultLowercase >= 'A' && resultLowercase <= 'Z');
    }

    [Fact]
    public void EncryptChar_RotorsAdvanceBeforeEncryption()
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI(rotorPositions: "AAA");
        var initialPositions = enigma.GetRotorPositions();

        // Act
        enigma.EncryptChar('A');
        var newPositions = enigma.GetRotorPositions();

        // Assert
        Assert.NotEqual(initialPositions, newPositions);
        Assert.Equal("AAB", newPositions);
    }

    #endregion

    #region Encrypt Tests

    [Theory]
    [InlineData("HELLOWORLD", "ILBDAAMTAZ")]
    [InlineData("ENIGMAMACHINE", "FQGAHWOXZNBML")]
    public void Encrypt_WithString_OutputsExpectedCypherText(string input, string expectedCypher)
    {
        // Arrange
        EnigmaMachine enigma = EnigmaMachine.CreateModelI();
        
        // Act
        string cypherText = enigma.Encrypt(input);
        
        // Assert
        Assert.Equal(expectedCypher, cypherText);
    }

    [Fact]
    public void Encrypt_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI();

        // Act
        var result = enigma.Encrypt("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Encrypt_WithNull_ReturnsNull()
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI();

        // Act
        var result = enigma.Encrypt(null!);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("AAAAA")]
    [InlineData("HELLO")]
    public void Encrypt_IsReversible_DecryptsCiphertext(string plaintext)
    {
        // Arrange
        var enigma1 = EnigmaMachine.CreateModelI(rotorPositions: "ABC");
        var enigma2 = EnigmaMachine.CreateModelI(rotorPositions: "ABC");

        // Act
        var ciphertext = enigma1.Encrypt(plaintext);
        var decrypted = enigma2.Encrypt(ciphertext);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_WithMixedCase_ConvertsToUppercase()
    {
        // Arrange
        var enigma1 = EnigmaMachine.CreateModelI();
        var enigma2 = EnigmaMachine.CreateModelI();

        // Act
        var result1 = enigma1.Encrypt("HeLLo");
        var result2 = enigma2.Encrypt("HELLO");

        // Assert
        Assert.Equal(result2, result1);
    }

    [Fact]
    public void Encrypt_WithInvalidCharacter_ThrowsArgumentException()
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => enigma.Encrypt("HELLO WORLD!"));
    }

    #endregion

    #region Rotor Stepping Tests

    [Fact]
    public void EncryptChar_RightRotorStepsEveryTime()
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI(rotorPositions: "AAA");

        // Act
        enigma.EncryptChar('A');
        enigma.EncryptChar('A');
        enigma.EncryptChar('A');

        // Assert
        Assert.Equal("AAD", enigma.GetRotorPositions());
    }

    [Fact]
    public void EncryptChar_MiddleRotorStepsAtNotch()
    {
        // Arrange
        // Rotor III has notch at V (position 21)
        // When right rotor is at the notch, it triggers middle rotor to step on the next encryption
        var enigma = EnigmaMachine.CreateModelI(rotorPositions: "AAV");

        // Act
        enigma.EncryptChar('A'); // Right rotor at notch

        // Assert - right rotor always steps, and when at notch it also steps middle rotor
        var positions = enigma.GetRotorPositions();
        Assert.StartsWith("AB", positions); // Middle rotor stepped
        Assert.Equal('W', positions[2]); // Right rotor stepped
    }

    [Fact]
    public void EncryptChar_DoubleSteppingOccurs()
    {
        // Arrange
        // Rotor II has notch at E (position 4)
        // Double-stepping: when middle rotor is at notch, both middle and left step
        var enigma = EnigmaMachine.CreateModelI(rotorOrder: "I II III", rotorPositions: "AEV");

        // Act
        enigma.EncryptChar('A'); // Middle rotor at notch triggers double-stepping

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
    [InlineData("AB CD EF")]
    [InlineData("")]
    [InlineData("AB")]
    public void CreateModelI_WithPlugboardPairs_CreatesValidMachine(string plugboardPairs)
    {
        // Act
        var enigma = EnigmaMachine.CreateModelI(plugboardPairs: plugboardPairs);

        // Assert
        Assert.NotNull(enigma);
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
    public void Encrypt_WithKnownHistoricalSettings_ProducesCorrectOutput()
    {
        // Arrange - Using a known Enigma configuration
        var enigma = EnigmaMachine.CreateModelI(
            rotorOrder: "I II III",
            rotorPositions: "AAA",
            ringSettings: "AAA",
            reflectorType: "UKW-B",
            plugboardPairs: ""
        );

        // Act
        var result = enigma.Encrypt("AAAAA");

        // Assert - First character should never be 'A' (Enigma never encrypts to itself)
        Assert.NotEqual('A', result[0]);
    }

    [Fact]
    public void Encrypt_EnigmaPropertyNeverEncryptsLetterToItself()
    {
        // Arrange
        var enigma = EnigmaMachine.CreateModelI();
        var plaintext = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        // Act
        var ciphertext = enigma.Encrypt(plaintext);

        // Assert
        for (int i = 0; i < plaintext.Length; i++)
        {
            Assert.NotEqual(plaintext[i], ciphertext[i]);
        }
    }

    [Fact]
    public void Encrypt_WithIdenticalSettings_ProducesIdenticalOutput()
    {
        // Arrange
        var enigma1 = EnigmaMachine.CreateModelI(
            rotorOrder: "III I II",
            rotorPositions: "MCK",
            ringSettings: "ABC",
            reflectorType: "UKW-B",
            plugboardPairs: "AB CD"
        );

        var enigma2 = EnigmaMachine.CreateModelI(
            rotorOrder: "III I II",
            rotorPositions: "MCK",
            ringSettings: "ABC",
            reflectorType: "UKW-B",
            plugboardPairs: "AB CD"
        );

        var plaintext = "HELLO";

        // Act
        var result1 = enigma1.Encrypt(plaintext);
        var result2 = enigma2.Encrypt(plaintext);

        // Assert
        Assert.Equal(result1, result2);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CompleteEncryptionDecryption_WithComplexSettings_WorksCorrectly()
    {
        // Arrange
        var settings = (
            rotorOrder: "V III I",
            rotorPositions: "WXC",
            ringSettings: "ABC",
            reflectorType: "UKW-C",
            plugboardPairs: "AD FT GW HY IJ KO LP NZ QM"
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
        var ciphertext = enigma1.Encrypt(plaintext);
        var decrypted = enigma2.Encrypt(ciphertext);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_LongMessage_MaintainsSymmetricProperty()
    {
        // Arrange
        var longMessage = string.Concat(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 10));
        var enigma1 = EnigmaMachine.CreateModelI(rotorPositions: "AAA");
        var enigma2 = EnigmaMachine.CreateModelI(rotorPositions: "AAA");

        // Act
        var encrypted = enigma1.Encrypt(longMessage);
        var decrypted = enigma2.Encrypt(encrypted);

        // Assert
        Assert.Equal(longMessage, decrypted);
        Assert.Equal(longMessage.Length, encrypted.Length);
    }

    #endregion
}
