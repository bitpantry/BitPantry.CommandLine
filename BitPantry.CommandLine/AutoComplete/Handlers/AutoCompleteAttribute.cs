using System;

namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Explicitly specifies which handler to use for this argument (Attribute Handler).
/// Overrides any Type Handler.
/// Generic constraint provides compile-time type safety.
/// Supports inheritance for syntactic-sugar custom attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AutoCompleteAttribute<THandler> : Attribute, IAutoCompleteAttribute
    where THandler : IAutoCompleteHandler
{
    /// <summary>
    /// Gets the type of the handler to use for autocomplete.
    /// </summary>
    public Type HandlerType => typeof(THandler);
}
