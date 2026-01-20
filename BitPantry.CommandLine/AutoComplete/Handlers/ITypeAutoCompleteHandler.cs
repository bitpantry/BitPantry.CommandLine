using System;

namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Extended interface for Type Handlers with runtime type matching.
/// Matching logic is encapsulated in CanHandle method.
/// </summary>
public interface ITypeAutoCompleteHandler : IAutoCompleteHandler
{
    /// <summary>
    /// Determines if this handler can handle the given argument type.
    /// Called at runtime to find matching handler.
    /// </summary>
    /// <param name="argumentType">The CLR type of the argument (nullable unwrapped)</param>
    /// <returns>True if this handler can handle the type</returns>
    bool CanHandle(Type argumentType);
}
