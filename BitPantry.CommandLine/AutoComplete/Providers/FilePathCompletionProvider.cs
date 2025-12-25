using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides file path completions for file system navigation.
/// </summary>
/// <remarks>
/// Implementation will be added in Phase 5 (User Story 4).
/// </remarks>
public sealed class FilePathCompletionProvider : ICompletionProvider
{
    /// <inheritdoc />
    public int Priority => 20;

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        // Handles when attribute is FilePathCompletionAttribute
        return context.CompletionAttribute is Attributes.FilePathCompletionAttribute;
    }

    /// <inheritdoc />
    public Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        // Implementation will be added in T036-T039
        return Task.FromResult(CompletionResult.Empty);
    }
}
