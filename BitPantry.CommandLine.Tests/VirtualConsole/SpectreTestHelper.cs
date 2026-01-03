using Spectre.Console;
using Spectre.Console.Testing;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.VirtualConsole;

/// <summary>
/// Simple test helper for "run to completion" style tests using Spectre's TestConsole.
/// This complements StepwiseTestRunner for scenarios where only the final result matters.
/// 
/// Use this for:
/// - Happy path tests where intermediate states don't need verification
/// - Snapshot testing where we capture final ANSI output
/// - Quick integration tests
/// 
/// Use StepwiseTestRunner for:
/// - Debugging complex interaction sequences
/// - Verifying intermediate visual states
/// - Step-by-step cursor position assertions
/// </summary>
public class SpectreTestHelper : IDisposable
{
    private readonly ConsolidatedTestConsole _console;
    private readonly TestConsoleInput _input;
    
    /// <summary>
    /// Gets the underlying test console.
    /// </summary>
    public ConsolidatedTestConsole Console => _console;
    
    /// <summary>
    /// Gets the raw output from the console.
    /// </summary>
    public string Output => _console.Output;
    
    /// <summary>
    /// Gets the output split into lines.
    /// </summary>
    public IReadOnlyList<string> Lines => _console.Lines;
    
    /// <summary>
    /// Creates a new SpectreTestHelper with default configuration.
    /// </summary>
    public SpectreTestHelper()
    {
        _console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();
        _input = _console.Input;
    }
    
    /// <summary>
    /// Queues text input followed by Enter.
    /// </summary>
    public SpectreTestHelper QueueInput(string text)
    {
        _input.PushTextWithEnter(text);
        return this;
    }
    
    /// <summary>
    /// Queues a key press.
    /// </summary>
    public SpectreTestHelper QueueKey(ConsoleKey key)
    {
        _input.PushKey(key);
        return this;
    }
    
    /// <summary>
    /// Queues multiple key presses.
    /// </summary>
    public SpectreTestHelper QueueKeys(params ConsoleKey[] keys)
    {
        foreach (var key in keys)
        {
            _input.PushKey(key);
        }
        return this;
    }
    
    /// <summary>
    /// Queues text without Enter.
    /// </summary>
    public SpectreTestHelper QueueText(string text)
    {
        _input.PushText(text);
        return this;
    }
    
    /// <summary>
    /// Gets the console as IAnsiConsole for use with InputBuilder or other components.
    /// </summary>
    public IAnsiConsole GetConsole() => _console;
    
    /// <inheritdoc/>
    public void Dispose()
    {
        _console.Dispose();
    }
}
