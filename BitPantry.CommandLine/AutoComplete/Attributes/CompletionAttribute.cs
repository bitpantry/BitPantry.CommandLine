using System;

namespace BitPantry.CommandLine.AutoComplete.Attributes;

/// <summary>
/// Specifies how completion suggestions are provided for a command argument.
/// This is the unified attribute for all completion scenarios.
/// </summary>
/// <remarks>
/// Three constructor overloads support different scenarios:
/// <list type="bullet">
///   <item>Method-based: Single string naming a method on the command class.</item>
///   <item>Static values: Two or more strings as fixed completion options.</item>
///   <item>Provider type: A Type implementing ICompletionProvider.</item>
/// </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class CompletionAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the method on the command class that provides completions.
    /// </summary>
    public string? MethodName { get; }

    /// <summary>
    /// Gets the static values for completion.
    /// </summary>
    public string[]? Values { get; }

    /// <summary>
    /// Gets the type that implements <see cref="Providers.ICompletionProvider"/>.
    /// </summary>
    public Type? ProviderType { get; }

    /// <summary>
    /// Gets or sets the cache duration in seconds. Default is 0 (no caching).
    /// </summary>
    public int CacheSeconds { get; set; }

    /// <summary>
    /// Gets or sets whether to disable ghost text for this completion.
    /// </summary>
    public bool DisableGhostText { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items to display in the menu.
    /// </summary>
    public int MaxDisplayItems { get; set; } = 10;

    /// <summary>
    /// Initializes a new instance with a method name for method-based completion.
    /// </summary>
    /// <param name="methodName">The name of a method on the command class that returns completion items.</param>
    /// <remarks>
    /// The method must have one of these signatures:
    /// <code>
    /// IEnumerable&lt;string&gt; MethodName()
    /// IEnumerable&lt;CompletionItem&gt; MethodName()
    /// Task&lt;IEnumerable&lt;string&gt;&gt; MethodName()
    /// Task&lt;IEnumerable&lt;CompletionItem&gt;&gt; MethodName()
    /// </code>
    /// The method receives the current context through a CompletionContext parameter.
    /// </remarks>
    public CompletionAttribute(string methodName)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
    }

    /// <summary>
    /// Initializes a new instance with static string values for completion.
    /// </summary>
    /// <param name="value1">First completion value.</param>
    /// <param name="value2">Second completion value.</param>
    /// <param name="moreValues">Additional completion values.</param>
    public CompletionAttribute(string value1, string value2, params string[] moreValues)
    {
        var allValues = new string[2 + moreValues.Length];
        allValues[0] = value1 ?? throw new ArgumentNullException(nameof(value1));
        allValues[1] = value2 ?? throw new ArgumentNullException(nameof(value2));
        Array.Copy(moreValues, 0, allValues, 2, moreValues.Length);
        Values = allValues;
    }

    /// <summary>
    /// Initializes a new instance with an array of static string values for completion.
    /// </summary>
    /// <param name="values">Array of completion values.</param>
    public CompletionAttribute(string[] values)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
    }

    /// <summary>
    /// Initializes a new instance with a custom provider type.
    /// </summary>
    /// <param name="providerType">A type implementing <see cref="Providers.ICompletionProvider"/>.</param>
    public CompletionAttribute(Type providerType)
    {
        ProviderType = providerType ?? throw new ArgumentNullException(nameof(providerType));
    }
}
