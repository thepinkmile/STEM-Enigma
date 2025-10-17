namespace CodeBreakers.Enigma;

/// <summary>
/// Examples of how to use the Enigma Model I machine implementation.
/// </summary>
[Obsolete("Remove leter when no longer looking at it for reference")]
public static class EnigmaExamples
{
    /// <summary>
    /// Basic example: Encrypt and decrypt a message using default settings.
    /// </summary>
    public static void BasicExample()
    {
        // Create a basic Enigma machine with default settings
        var enigma = EnigmaMachine.CreateModelI();

        // Encrypt a message
        string plaintext = "HELLO WORLD";
        string ciphertext = enigma.Encrypt(plaintext);
        
        Console.WriteLine($"Plaintext:  {plaintext}");
        Console.WriteLine($"Ciphertext: {ciphertext}");

        // To decrypt, reset the machine to the same starting position
        enigma.SetRotorPositions(0, 0, 0); // Reset to AAA
        string decrypted = enigma.Encrypt(ciphertext);
        
        Console.WriteLine($"Decrypted:  {decrypted}");
    }

    /// <summary>
    /// Advanced example: Custom rotor order, positions, ring settings, and plugboard.
    /// </summary>
    public static void AdvancedExample()
    {
        // Historical configuration example
        var enigma = EnigmaMachine.CreateModelI(
            rotorOrder: "II IV V",        // Rotor selection (left to right)
            rotorPositions: "BUL",        // Initial positions
            ringSettings: "AAV",          // Ring settings
            reflectorType: "UKW-B",       // Reflector type (B was most common)
            plugboardPairs: "AV BS CG DL FU HZ IN KM OW RX" // 10 plugboard pairs
        );

        string message = "ENIGMA CIPHER MACHINE";
        string encrypted = enigma.Encrypt(message);
        
        Console.WriteLine($"Original:  {message}");
        Console.WriteLine($"Encrypted: {encrypted}");
        Console.WriteLine($"Rotor positions after encryption: {enigma.GetRotorPositions()}");

        // To decrypt, create another machine with the same initial settings
        var decryptMachine = EnigmaMachine.CreateModelI(
            rotorOrder: "II IV V",
            rotorPositions: "BUL",
            ringSettings: "AAV",
            reflectorType: "UKW-B",
            plugboardPairs: "AV BS CG DL FU HZ IN KM OW RX"
        );
        
        string decrypted = decryptMachine.Encrypt(encrypted);
        Console.WriteLine($"Decrypted: {decrypted}");
    }

    /// <summary>
    /// Custom rotor configuration example using individual components.
    /// </summary>
    public static void CustomExample()
    {
        // Create individual components
        var leftRotor = Rotor.RotorType.I(position: 0, ringSetting: 0);
        var middleRotor = Rotor.RotorType.II(position: 0, ringSetting: 0);
        var rightRotor = Rotor.RotorType.III(position: 0, ringSetting: 0);
        var reflector = Reflector.ReflectorType.UKW_B();
        var plugboard = new Plugboard("AB CD EF GH IJ");

        // Create the machine
        var enigma = new EnigmaMachine(
            leftRotor,
            middleRotor,
            rightRotor,
            reflector,
            plugboard
        );

        // Use the machine
        string message = "SECRETMESSAGE";
        string encrypted = enigma.Encrypt(message);
        
        Console.WriteLine($"Message:   {message}");
        Console.WriteLine($"Encrypted: {encrypted}");
    }

    /// <summary>
    /// Historical message example: Operation Barbarossa message (22 June 1941).
    /// </summary>
    public static void HistoricalExample()
    {
        // This is a simplified example based on historical Enigma usage
        var enigma = EnigmaMachine.CreateModelI(
            rotorOrder: "II IV V",
            rotorPositions: "BLA",
            ringSettings: "BUL",
            reflectorType: "UKW-B",
            plugboardPairs: "AV BS CG DL FU HZ IN KM OW RX"
        );

        string message = "OPERATION BARBAROSSA BEGINS AT DAWN";
        Console.WriteLine($"Original message: {message}");
        
        string encrypted = enigma.Encrypt(message);
        Console.WriteLine($"Encrypted:        {encrypted}");
        Console.WriteLine($"Final positions:  {enigma.GetRotorPositions()}");
    }
}
