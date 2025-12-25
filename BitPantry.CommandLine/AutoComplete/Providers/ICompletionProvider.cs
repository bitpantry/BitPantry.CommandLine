using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completion suggestions for a specific context.
/// All completion providers (built-in and custom) implement this interface.
/// </summary>
public interface ICompletionProvider
{
    /// <summary>
    /// Gets the priority of this provider. Higher values execute first.
    /// </summary>
    /// <remarks>
    /// Default is 0. Built-in providers use priorities:
    /// - Custom providers: 50+
    /// - Attribute-based (File/Directory/Values): 20
    /// - Enum: 10
    /// - Default (Command/Argument): 0
    /// </remarks>
    int Priority => 0;

    /// <summary>
    /// Determines if this provider can handle the given context.
    /// </summary>
    /// <param name="context">The completion context.</param>
    /// <returns>True if this provider should be used for completions.</returns>
    bool CanHandle(CompletionContext context);

    /// <summary>
    /// Gets completion suggestions for the given context.
    /// </summary>
    /// <param name="context">The completion context containing input state.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The completion result containing suggestions.</returns>
    Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken = default);
}
