using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Description;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completion suggestions for positional argument values.
/// </summary>
/// <remarks>
/// This provider handles completion when the user is at a positional slot.
/// It invokes the [Completion] attribute's method to get suggestions for
/// the current positional argument.
/// 
/// Per FR-024a, this provider is used when there's no dash prefix (-- or -).
/// Per FR-024b, if no positional completions are available, the system falls
/// back to ArgumentName completion.
/// </remarks>
public class PositionalArgumentProvider : ICompletionProvider
{
    private readonly CommandRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionalArgumentProvider"/> class.
    /// </summary>
    /// <param name="registry">The command registry for resolving command definitions.</param>
    public PositionalArgumentProvider(CommandRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Priority 45 - after commands (0), before argument names (50).
    /// This ensures positional completion is tried before falling back to options.
    /// </remarks>
    public int Priority => 45;

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        return context.ElementType == CompletionElementType.Positional;
    }

    /// <inheritdoc />
    public async Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return CompletionResult.Empty;

        if (string.IsNullOrEmpty(context.CommandName))
            return CompletionResult.Empty;

        // Get the command definition
        var commandInfo = FindCommand(context.CommandName);
        if (commandInfo == null)
            return CompletionResult.Empty;

        // The orchestrator passes the positional slot index in ArgumentName field
        // If not available, fall back to calculating it
        int positionalIndex;
        if (int.TryParse(context.ArgumentName, out var slotFromContext))
        {
            positionalIndex = slotFromContext;
        }
        else
        {
            positionalIndex = GetCurrentPositionalIndex(context, commandInfo);
        }

        if (positionalIndex < 0)
            return CompletionResult.Empty;

        // Find the positional argument at this index
        var positionalArg = GetPositionalArgumentAtIndex(commandInfo, positionalIndex);
        if (positionalArg == null)
            return CompletionResult.Empty;

        // Check if this positional has a completion attribute
        if (positionalArg.CompletionAttribute == null)
            return CompletionResult.Empty;

        // Get completions based on the type of completion attribute
        var items = await GetCompletionsFromAttributeAsync(
            commandInfo, 
            positionalArg, 
            context, 
            cancellationToken);

        if (items == null || !items.Any())
            return CompletionResult.Empty;

        // Filter by partial value if provided
        var prefix = context.PartialValue ?? string.Empty;
        if (!string.IsNullOrEmpty(prefix))
        {
            items = items.Where(item =>
                item.InsertText?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true ||
                item.DisplayText?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true);
        }

        var itemList = items.ToList();

        return new CompletionResult(itemList);
    }

    /// <summary>
    /// Finds a command by name (handles both simple and fully-qualified names).
    /// </summary>
    private CommandInfo FindCommand(string commandName)
    {
        return _registry.Commands.FirstOrDefault(c =>
            string.Equals(c.Name, commandName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.FullyQualifiedName, commandName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines the current positional index based on what's already entered.
    /// Accounts for positional arguments satisfied via named syntax (dual-mode).
    /// </summary>
    private int GetCurrentPositionalIndex(CompletionContext context, CommandInfo commandInfo)
    {
        var usedArgs = context.UsedArguments ?? new HashSet<string>();
        
        // Get all positional arguments ordered by position
        var positionalArgs = commandInfo.Arguments
            .Where(a => a.IsPositional)
            .OrderBy(a => a.Position)
            .ToList();

        if (!positionalArgs.Any())
            return -1;

        // Count positional values already provided in the input
        // This is a simplified approach - we count space-separated values that aren't options
        var positionalValuesProvided = CountPositionalValuesProvided(context, commandInfo);

        // Also account for positional args filled via named syntax (--Source value)
        var filledViaName = positionalArgs
            .Where(a => usedArgs.Contains(a.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // The effective positional index is the count of positional values
        // minus those that were satisfied by position (not by name)
        // But we need to find the first UNFILLED positional slot
        for (int i = 0; i < positionalArgs.Count; i++)
        {
            var arg = positionalArgs[i];
            
            // Check if this position is filled via named syntax
            if (usedArgs.Contains(arg.Name, StringComparer.OrdinalIgnoreCase))
                continue;

            // Check if this position is filled via positional value
            if (i < positionalValuesProvided)
                continue;

            // Check for IsRest - if previous IsRest is defined, it consumes all remaining
            if (arg.IsRest || i == positionalValuesProvided)
                return arg.Position;
        }

        // If all non-IsRest positions are filled, check for IsRest
        var restArg = positionalArgs.FirstOrDefault(a => a.IsRest);
        if (restArg != null)
        {
            // IsRest can always accept more values
            return restArg.Position;
        }

        // All positional slots filled
        return -1;
    }

    /// <summary>
    /// Counts positional values already provided in the input.
    /// </summary>
    private int CountPositionalValuesProvided(CompletionContext context, CommandInfo commandInfo)
    {
        if (string.IsNullOrWhiteSpace(context.InputText))
            return 0;

        var parts = context.InputText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Skip command parts
        var commandParts = (context.CommandName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var argsStart = commandParts;
        
        int positionalCount = 0;
        bool skipNextAsValue = false;

        for (int i = argsStart; i < parts.Length; i++)
        {
            var part = parts[i];

            if (skipNextAsValue)
            {
                skipNextAsValue = false;
                continue;
            }

            if (part.StartsWith("--") || part.StartsWith("-"))
            {
                // This is an option - check if it takes a value
                var argName = part.TrimStart('-');
                var argInfo = commandInfo.Arguments.FirstOrDefault(a =>
                    string.Equals(a.Name, argName, StringComparison.OrdinalIgnoreCase) ||
                    (argName.Length == 1 && a.Alias == argName[0]));

                if (argInfo != null && !argInfo.IsOption)
                {
                    // Non-flag option takes a value, skip next part
                    skipNextAsValue = true;
                }
                continue;
            }

            // This is a positional value
            positionalCount++;
        }

        // Don't count the current partial value being typed
        if (!context.InputText.EndsWith(" ") && !string.IsNullOrEmpty(context.PartialValue))
        {
            positionalCount = Math.Max(0, positionalCount - 1);
        }

        return positionalCount;
    }

    /// <summary>
    /// Gets completions from the attribute based on its type (ProviderType, Values, or MethodName).
    /// </summary>
    private async Task<IEnumerable<CompletionItem>> GetCompletionsFromAttributeAsync(
        CommandInfo commandInfo,
        ArgumentInfo argumentInfo,
        CompletionContext context,
        CancellationToken cancellationToken)
    {
        var completionAttr = argumentInfo.CompletionAttribute;
        if (completionAttr == null)
            return null;

        // 1. Handle ProviderType - invoke the custom provider from DI
        if (completionAttr.ProviderType != null)
        {
            return await InvokeProviderTypeAsync(completionAttr.ProviderType, argumentInfo, context, cancellationToken);
        }

        // 2. Handle Values - return static values
        if (completionAttr.Values != null && completionAttr.Values.Length > 0)
        {
            return completionAttr.Values.Select(v => new CompletionItem
            {
                DisplayText = v,
                InsertText = v,
                Kind = CompletionItemKind.ArgumentValue,
                SortPriority = 0
            });
        }

        // 3. Handle MethodName - invoke the method on the command
        if (!string.IsNullOrEmpty(completionAttr.MethodName))
        {
            return await InvokeCompletionMethodAsync(commandInfo, argumentInfo, context, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Invokes a custom completion provider registered as a type.
    /// </summary>
    private async Task<IEnumerable<CompletionItem>> InvokeProviderTypeAsync(
        Type providerType,
        ArgumentInfo argumentInfo,
        CompletionContext context,
        CancellationToken cancellationToken)
    {
        if (context.Services == null)
            return null;

        try
        {
            // Try to get the provider from DI
            var provider = context.Services.GetService(providerType) as ICompletionProvider;
            if (provider == null)
            {
                // Try to create an instance if not registered in DI
                provider = ActivatorUtilities.CreateInstance(context.Services, providerType) as ICompletionProvider;
            }

            if (provider == null)
                return null;

            // Create a modified context with the completion attribute for the provider to check
            var providerContext = new CompletionContext
            {
                InputText = context.InputText,
                CursorPosition = context.CursorPosition,
                ElementType = CompletionElementType.ArgumentValue, // Provider expects ArgumentValue
                CommandName = context.CommandName,
                ArgumentName = argumentInfo.Name,
                PartialValue = context.PartialValue,
                CurrentWord = context.PartialValue,
                UsedArguments = context.UsedArguments,
                PropertyType = context.PropertyType,
                CompletionAttribute = argumentInfo.CompletionAttribute,
                IsRemote = context.IsRemote,
                Services = context.Services,
                CommandInstance = context.CommandInstance
            };

            var result = await provider.GetCompletionsAsync(providerContext, cancellationToken);
            return result?.Items;
        }
        catch (Exception)
        {
            // Provider invocation failed - return no results
            return null;
        }
    }

    /// <summary>
    /// Gets the positional argument at the specified position index.
    /// </summary>
    private ArgumentInfo GetPositionalArgumentAtIndex(CommandInfo commandInfo, int position)
    {
        return commandInfo.Arguments
            .FirstOrDefault(a => a.IsPositional && a.Position == position);
    }

    /// <summary>
    /// Invokes the completion method defined in the [Completion] attribute.
    /// </summary>
    private async Task<IEnumerable<CompletionItem>> InvokeCompletionMethodAsync(
        CommandInfo commandInfo,
        ArgumentInfo argumentInfo,
        CompletionContext context,
        CancellationToken cancellationToken)
    {
        var completionAttr = argumentInfo.CompletionAttribute;
        if (completionAttr == null || string.IsNullOrEmpty(completionAttr.MethodName))
            return null;

        try
        {
            // Find the method on the command type
            var method = commandInfo.Type.GetMethod(
                completionAttr.MethodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

            if (method == null)
                return null;

            // The completion method expects a CompletionContext directly
            // (same type used throughout the autocomplete system)
            object result;
            if (method.IsStatic)
            {
                result = method.Invoke(null, new object[] { context });
            }
            else
            {
                // Need to create an instance of the command
                var instance = Activator.CreateInstance(commandInfo.Type);
                result = method.Invoke(instance, new object[] { context });
            }

            // Handle async methods
            if (result is Task<IEnumerable<CompletionItem>> asyncResult)
            {
                return await asyncResult;
            }

            return result as IEnumerable<CompletionItem>;
        }
        catch (Exception)
        {
            // Completion method failed - return no results
            return null;
        }
    }
}
