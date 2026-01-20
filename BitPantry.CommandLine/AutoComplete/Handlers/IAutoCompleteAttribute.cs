using System;

namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Marker interface for AutoComplete attributes.
/// Enables reflection-based discovery of generic attributes.
/// </summary>
public interface IAutoCompleteAttribute
{
    /// <summary>
    /// Gets the type of the handler to use for autocomplete.
    /// </summary>
    Type HandlerType { get; }
}
