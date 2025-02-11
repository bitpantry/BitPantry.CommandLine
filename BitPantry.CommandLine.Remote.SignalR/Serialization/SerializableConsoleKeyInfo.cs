namespace BitPantry.CommandLine.Remote.SignalR.Serialization;

public class SerializableConsoleKeyInfo
{
    public ConsoleKey Key { get; set; }
    public char KeyChar { get; set; }
    public ConsoleModifiers Modifiers { get; set; }

    public SerializableConsoleKeyInfo() { }

    public SerializableConsoleKeyInfo(ConsoleKeyInfo? keyInfo)
    {
        Key = keyInfo.Value.Key;
        KeyChar = keyInfo.Value.KeyChar;
        Modifiers = keyInfo.Value.Modifiers;
    }

    public ConsoleKeyInfo ToKeyInfo()
        => new(KeyChar, Key, Modifiers.HasFlag(ConsoleModifiers.Shift), Modifiers.HasFlag(ConsoleModifiers.Alt), Modifiers.HasFlag(ConsoleModifiers.Control));
}
