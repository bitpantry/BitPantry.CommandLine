using System.Text;

namespace BitPantry.VirtualConsole.Testing;

public class VirtualConsoleWriteLog
{
    private readonly StringBuilder _writeLog = new();

    /// <summary>
    /// Gets all text written to the log.
    /// This includes raw ANSI sequences and content that was later cleared.
    /// Useful for verifying that transient UI elements (progress bars, spinners) were displayed.
    /// </summary>
    public string Contents => _writeLog.ToString();

    /// <summary>
    /// Clears the write log.
    /// </summary>
    public void Clear() => _writeLog.Clear();

    public void Append(string value) => _writeLog.Append(value);
    public void Append(char value) => _writeLog.Append(value);
}