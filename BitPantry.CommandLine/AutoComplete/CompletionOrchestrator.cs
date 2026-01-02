using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.Component;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Orchestrates the completion system, coordinating between providers,
/// cache, matching, and UI rendering.
/// </summary>
public class CompletionOrchestrator : ICompletionOrchestrator
{
    private readonly IEnumerable<ICompletionProvider> _providers;
    private readonly ICompletionCache _cache;
    private readonly CommandRegistry _registry;
    private readonly IServiceProvider _serviceProvider;

    private MenuState _menuState;
    private string _currentQuery = string.Empty;
    private List<CompletionItem> _allItems = new();
    private List<CompletionItem> _filteredItems = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CompletionOrchestrator"/> class.
    /// </summary>
    public CompletionOrchestrator(
        IEnumerable<ICompletionProvider> providers,
        ICompletionCache cache,
        CommandRegistry registry,
        IServiceProvider serviceProvider)
    {
        _providers = providers?.OrderByDescending(p => p.Priority) 
            ?? throw new ArgumentNullException(nameof(providers));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public bool IsMenuOpen => _menuState != null;

    /// <inheritdoc />
    public MenuState MenuState => _menuState;

    /// <inheritdoc />
    public async Task<CompletionAction> HandleTabAsync(
        string inputBuffer,
        int cursorPosition,
        CancellationToken cancellationToken = default)
    {
        if (IsMenuOpen)
        {
            // Tab while menu is open - move to next item
            return MoveSelection(1);
        }

        // Build completion context
        var context = BuildContext(inputBuffer, cursorPosition);

        // Try cache first - use FromContext to include ElementType in key
        // This ensures ArgumentName and ArgumentAlias completions are cached separately
        var cacheKey = CacheKey.FromContext(context);
        var cachedResult = _cache.Get(cacheKey);

        CompletionResult result;
        if (cachedResult != null)
        {
            result = cachedResult;
        }
        else
        {
            // Find the first provider that can handle this context
            result = await GetCompletionsFromProvidersAsync(context, cancellationToken);

            // FR-024b: Fallback to ArgumentName completion when Positional returns empty
            if (result.Items.Count == 0 && context.ElementType == CompletionElementType.Positional)
            {
                var fallbackContext = new CompletionContext
                {
                    InputText = context.InputText,
                    CursorPosition = context.CursorPosition,
                    ElementType = CompletionElementType.ArgumentName,
                    CommandName = context.CommandName,
                    ArgumentName = null,
                    PartialValue = context.PartialValue,
                    UsedArguments = context.UsedArguments,
                    PropertyType = context.PropertyType,
                    CompletionAttribute = null,
                    IsRemote = context.IsRemote,
                    Services = _serviceProvider
                };
                result = await GetCompletionsFromProvidersAsync(fallbackContext, cancellationToken);
            }

            // Cache the result
            if (!result.IsError && !result.IsTimedOut && result.Items.Count > 0)
            {
                _cache.Set(cacheKey, result);
            }
        }

        if (result.Items.Count == 0)
        {
            return CompletionAction.NoMatches();
        }

        if (result.Items.Count == 1)
        {
            // Single match - accept immediately
            return CompletionAction.Accept(result.Items[0].InsertText);
        }

        // Multiple matches - show menu
        _allItems = result.Items.ToList();
        _filteredItems = _allItems.ToList();
        _currentQuery = context.PartialValue ?? string.Empty;

        _menuState = new MenuState
        {
            Items = _filteredItems,
            SelectedIndex = 0,
            ViewportStart = 0,
            ViewportSize = Math.Min(10, _filteredItems.Count),
            TotalCount = result.TotalCount
        };

        return CompletionAction.ShowMenu(_menuState);
    }

    /// <inheritdoc />
    public Task<CompletionAction> HandleShiftTabAsync(CancellationToken cancellationToken = default)
    {
        if (!IsMenuOpen)
            return Task.FromResult(CompletionAction.None());

        return Task.FromResult(MoveSelection(-1));
    }

    /// <inheritdoc />
    public CompletionAction HandleEscape()
    {
        if (!IsMenuOpen)
            return CompletionAction.None();

        CloseMenu();
        return CompletionAction.Close();
    }

    /// <inheritdoc />
    public CompletionAction HandleEnter()
    {
        if (!IsMenuOpen || _menuState == null || _filteredItems.Count == 0)
            return CompletionAction.None();

        var selectedItem = _filteredItems[_menuState.SelectedIndex];
        CloseMenu();
        return CompletionAction.Accept(selectedItem.InsertText);
    }

    /// <inheritdoc />
    public CompletionAction HandleUpArrow()
    {
        if (!IsMenuOpen)
            return CompletionAction.None();

        return MoveSelection(-1);
    }

    /// <inheritdoc />
    public CompletionAction HandleDownArrow()
    {
        if (!IsMenuOpen)
            return CompletionAction.None();

        return MoveSelection(1);
    }

    /// <inheritdoc />
    public async Task<CompletionAction> HandleCharacterAsync(
        char character,
        string inputBuffer,
        int cursorPosition,
        CancellationToken cancellationToken = default)
    {
        if (!IsMenuOpen)
            return CompletionAction.None();

        // Update query and filter items
        _currentQuery += character;

        // Filter items using matcher (case-insensitive prefix matching)
        var matched = CompletionMatcher.Match(_allItems, _currentQuery, MatchMode.PrefixCaseInsensitive);
        _filteredItems = matched.ToList();

        if (_filteredItems.Count == 0)
        {
            CloseMenu();
            return CompletionAction.NoMatches();
        }

        // Update menu state
        _menuState = new MenuState
        {
            Items = _filteredItems,
            SelectedIndex = 0,
            ViewportStart = 0,
            ViewportSize = Math.Min(10, _filteredItems.Count),
            TotalCount = _filteredItems.Count
        };

        return await Task.FromResult(CompletionAction.UpdateMenu(_menuState));
    }

    /// <inheritdoc />
    public async Task<string?> UpdateGhostTextAsync(
        string inputBuffer,
        CancellationToken cancellationToken = default)
    {
        // Don't show ghost when menu is open
        if (IsMenuOpen)
            return null;

        if (string.IsNullOrEmpty(inputBuffer))
            return null;

        // Build context for ghost query
        var context = BuildContext(inputBuffer, inputBuffer.Length);

        // Query providers for ghost suggestion
        // History provider has higher priority and will be checked first
        foreach (var provider in _providers)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            if (!provider.CanHandle(context))
                continue;

            var result = await provider.GetCompletionsAsync(context, cancellationToken);
            
            if (result.Items.Count > 0)
            {
                var bestMatch = result.Items[0];
                var partialValue = context.PartialValue ?? string.Empty;
                
                // Determine the prefix based on element type
                // For argument names (--argName), prefix is "--"
                // For argument aliases (-a), prefix is "-"
                string prefix = context.ElementType switch
                {
                    CompletionElementType.ArgumentName => "--",
                    CompletionElementType.ArgumentAlias => "-",
                    _ => string.Empty
                };
                
                // For argument types, InsertText includes prefix (e.g., "--host", "-p")
                // Get the unprefixed portion for comparison
                var insertTextWithoutPrefix = !string.IsNullOrEmpty(prefix) && bestMatch.InsertText.StartsWith(prefix)
                    ? bestMatch.InsertText.Substring(prefix.Length)
                    : bestMatch.InsertText;
                
                // Check if the match starts with the partial value
                if (insertTextWithoutPrefix.StartsWith(partialValue, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(insertTextWithoutPrefix, partialValue, StringComparison.OrdinalIgnoreCase))
                {
                    // Check if user has already typed the prefix by looking at the input buffer
                    // e.g., "version --" ends with prefix, "server connect " does not
                    var userTypedPrefix = !string.IsNullOrEmpty(prefix) && 
                        (inputBuffer.EndsWith(prefix) || inputBuffer.EndsWith(prefix + partialValue));
                    
                    // If user hasn't typed the prefix yet, ghost should show full InsertText (e.g., "--host")
                    // If user has typed the prefix, ghost should show only the remainder (e.g., "host")
                    if (string.IsNullOrEmpty(partialValue) && !string.IsNullOrEmpty(prefix) && !userTypedPrefix)
                    {
                        return bestMatch.InsertText;
                    }
                    
                    // Otherwise, return just the remaining suffix after what's typed
                    return insertTextWithoutPrefix.Substring(partialValue.Length);
                }
            }
        }

        return null;
    }

    /// <inheritdoc />
    public void InvalidateCacheForCommand(string commandName)
    {
        // Invalidate all cache entries for this command
        _cache.InvalidateForCommand(commandName);
    }

    /// <summary>
    /// Builds a completion context from the current input state.
    /// </summary>
    private CompletionContext BuildContext(string inputBuffer, int cursorPosition)
    {
        var (elementType, commandName, argumentName, partialValue) = ParseInput(inputBuffer, cursorPosition);

        // Track which arguments have already been used
        var usedArguments = UsedArgumentTracker.GetUsedArguments(inputBuffer, cursorPosition);

        // Also track positional arguments that have been filled by positional values
        if (!string.IsNullOrEmpty(commandName))
        {
            var filledPositionalArgs = GetFilledPositionalArgumentNames(commandName, inputBuffer, cursorPosition);
            foreach (var argName in filledPositionalArgs)
            {
                usedArguments.Add(argName);
            }
        }

        // Look up argument info for property type and completion attribute
        Type propertyType = null;
        Attributes.CompletionAttribute completionAttribute = null;
        bool isRemote = false;

        // Look up command info to determine if it's remote
        Component.CommandInfo commandInfo = null;
        if (!string.IsNullOrEmpty(commandName))
        {
            commandInfo = _registry.Commands.FirstOrDefault(c =>
                string.Equals(c.Name, commandName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.FullyQualifiedName, commandName, StringComparison.OrdinalIgnoreCase));
            
            if (commandInfo != null)
            {
                isRemote = commandInfo.IsRemote;
            }
        }

        if (!string.IsNullOrEmpty(commandName) && !string.IsNullOrEmpty(argumentName) &&
            elementType == CompletionElementType.ArgumentValue)
        {
            var argumentInfo = FindArgumentInfo(commandName, argumentName);
            if (argumentInfo != null)
            {
                // Get property type from PropertyInfo
                if (argumentInfo.PropertyInfo?.PropertyTypeName != null)
                {
                    propertyType = Type.GetType(argumentInfo.PropertyInfo.PropertyTypeName);
                }
                
                // Get completion attribute
                completionAttribute = argumentInfo.CompletionAttribute;
            }
        }

        return new CompletionContext
        {
            InputText = inputBuffer ?? string.Empty,
            CursorPosition = cursorPosition,
            ElementType = elementType,
            CommandName = commandName,
            ArgumentName = argumentName,
            PartialValue = partialValue,
            UsedArguments = usedArguments,
            PropertyType = propertyType,
            CompletionAttribute = completionAttribute,
            IsRemote = isRemote,
            Services = _serviceProvider
        };
    }

    /// <summary>
    /// Finds argument info for a given command and argument name.
    /// </summary>
    private Component.ArgumentInfo FindArgumentInfo(string commandName, string argumentName)
    {
        // Find the command by name (handles both simple and fully-qualified names)
        var command = _registry.Commands.FirstOrDefault(c =>
            string.Equals(c.Name, commandName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.FullyQualifiedName, commandName, StringComparison.OrdinalIgnoreCase));

        if (command == null)
            return null;

        // Find the argument by name or alias
        foreach (var arg in command.Arguments)
        {
            if (string.Equals(arg.Name, argumentName, StringComparison.OrdinalIgnoreCase))
                return arg;
            
            // Check alias (single character)
            if (argumentName.Length == 1 && arg.Alias == argumentName[0])
                return arg;
        }

        return null;
    }

    /// <summary>
    /// Checks if the specified argument is an Option type (boolean flag that takes no value).
    /// </summary>
    /// <param name="commandName">The command name.</param>
    /// <param name="argumentName">The argument name or alias.</param>
    /// <returns>True if the argument is an Option type, false otherwise.</returns>
    private bool IsOptionTypeArgument(string commandName, string argumentName)
    {
        if (string.IsNullOrEmpty(commandName) || string.IsNullOrEmpty(argumentName))
            return false;

        var argInfo = FindArgumentInfo(commandName, argumentName);
        return argInfo?.IsOption == true;
    }

    /// <summary>
    /// Parses the input to determine what kind of completion is needed.
    /// </summary>
    private (CompletionElementType, string, string, string) ParseInput(string inputBuffer, int cursorPosition)
    {
        if (string.IsNullOrWhiteSpace(inputBuffer))
            return (CompletionElementType.Empty, null, null, string.Empty);

        var textBeforeCursor = inputBuffer.Substring(0, Math.Min(cursorPosition, inputBuffer.Length));
        var parts = textBeforeCursor.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return (CompletionElementType.Empty, null, null, string.Empty);

        // Check if input ends with space - we're starting a new token
        var endsWithSpace = textBeforeCursor.EndsWith(" ");

        var lastPart = endsWithSpace ? string.Empty : parts[^1];
        var partsForContext = endsWithSpace ? parts : parts.Take(parts.Length - 1).ToArray();

        // Check for argument name completion (starts with --)
        if (!endsWithSpace && lastPart.StartsWith("--"))
        {
            var commandName = FindCommandName(parts.Take(parts.Length - 1).ToArray());
            return (CompletionElementType.ArgumentName, commandName, null, lastPart.Substring(2));
        }

        // Check for argument alias completion (starts with -)
        if (!endsWithSpace && lastPart.StartsWith("-") && !lastPart.StartsWith("--"))
        {
            var commandName = FindCommandName(parts.Take(parts.Length - 1).ToArray());
            return (CompletionElementType.ArgumentAlias, commandName, null, lastPart.Substring(1));
        }

        // Check if previous part is an argument name (expecting value)
        var prevPartIndex = endsWithSpace ? parts.Length - 1 : parts.Length - 2;
        if (prevPartIndex >= 0)
        {
            var prevPart = parts[prevPartIndex];
            if (prevPart.StartsWith("--") || prevPart.StartsWith("-"))
            {
                var commandName = FindCommandName(parts.Take(prevPartIndex).ToArray());
                var argName = prevPart.TrimStart('-');
                
                // Check if this argument is an Option type (boolean flag) - these don't take values
                if (!IsOptionTypeArgument(commandName, argName))
                {
                    return (CompletionElementType.ArgumentValue, commandName, argName, lastPart);
                }
                // For Option type, fall through to argument name completion
            }
        }

        // Check if we're after a complete command (should complete arguments, not commands)
        // This handles cases like "server connect " or "copy fil" for positional values
        var fullCommandName = FindCommandName(parts);
        if (!string.IsNullOrEmpty(fullCommandName))
        {
            // Get the argument parts (everything after the command)
            var commandParts = fullCommandName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var argParts = parts.Skip(commandParts.Length).ToArray();

            // FR-024a: Prefix-driven intent detection
            // If user is typing something without a dash prefix, check for positional completion
            if (endsWithSpace)
            {
                // User pressed Tab after space - check for unfilled positional slots
                if (HasUnfilledPositionalSlots(fullCommandName, argParts))
                {
                    var slot = GetCurrentPositionalSlot(fullCommandName, argParts);
                    return (CompletionElementType.Positional, fullCommandName, slot.ToString(), string.Empty);
                }
                // Fall back to argument name completion if no positional slots available
                return (CompletionElementType.ArgumentName, fullCommandName, null, string.Empty);
            }
            else if (!lastPart.StartsWith("-"))
            {
                // User is typing something without dash - treat as positional value partial
                // argParts includes the partial being typed, so exclude it for slot calculation
                var partsForSlotCalc = argParts.Length > 0 ? argParts.Take(argParts.Length - 1).ToArray() : argParts;
                if (HasUnfilledPositionalSlots(fullCommandName, partsForSlotCalc))
                {
                    var slot = GetCurrentPositionalSlot(fullCommandName, partsForSlotCalc);
                    return (CompletionElementType.Positional, fullCommandName, slot.ToString(), lastPart);
                }
                // No positional slots - fall through to command completion (for partial command names)
            }
        }

        // Default to command completion
        // Check if this might be within a group context
        var commandPartsForGroup = new List<string>(parts);
        var partial = endsWithSpace ? string.Empty : commandPartsForGroup[^1];

        // Try to find if we're in a group context
        var groupPath = string.Empty;
        var partsToCheck = endsWithSpace ? commandPartsForGroup.Count : commandPartsForGroup.Count - 1;
        for (int i = 0; i < partsToCheck; i++)
        {
            var potentialGroup = string.IsNullOrEmpty(groupPath) 
                ? commandPartsForGroup[i] 
                : $"{groupPath} {commandPartsForGroup[i]}";
            
            if (IsGroup(potentialGroup))
            {
                groupPath = potentialGroup;
            }
            else
            {
                break;
            }
        }

        return (CompletionElementType.Command, groupPath, null, partial);
    }

    /// <summary>
    /// Finds the command name from an array of input parts.
    /// </summary>
    private string FindCommandName(string[] parts)
    {
        if (parts.Length == 0)
            return null;

        // Walk through parts to find the command
        // Could be "group subgroup command" or just "command"
        var groupPath = new List<string>();
        string commandName = null;

        foreach (var part in parts)
        {
            var testPath = groupPath.Count > 0 
                ? string.Join(" ", groupPath) + " " + part 
                : part;

            if (IsGroup(testPath) || IsGroup(part))
            {
                groupPath.Add(part);
            }
            else if (IsCommand(testPath) || IsCommand(part))
            {
                commandName = part;
                break;
            }
        }

        if (commandName != null && groupPath.Count > 0)
        {
            return string.Join(" ", groupPath) + " " + commandName;
        }

        return commandName;
    }

    /// <summary>
    /// Checks if the given name is a registered group.
    /// </summary>
    private bool IsGroup(string name)
    {
        return _registry.Groups.Any(g => 
            string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(g.FullPath, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the given name is a registered command.
    /// </summary>
    private bool IsCommand(string name)
    {
        return _registry.Commands.Any(c => 
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a command by name from the registry.
    /// </summary>
    private Component.CommandInfo GetCommand(string name)
    {
        return _registry.Commands.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.FullyQualifiedName, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if there are unfilled positional argument slots for the command.
    /// Takes into account positional arguments filled via named syntax (e.g., --Source).
    /// </summary>
    /// <param name="commandName">The command name.</param>
    /// <param name="inputParts">The parts of the input after the command.</param>
    /// <returns>True if there are positional slots that can accept values.</returns>
    private bool HasUnfilledPositionalSlots(string commandName, string[] inputParts)
    {
        var command = GetCommand(commandName);
        if (command == null)
            return false;

        var positionalArgs = command.Arguments
            .Where(a => a.IsPositional)
            .OrderBy(a => a.Position)
            .ToList();

        if (!positionalArgs.Any())
            return false;

        // Track which positional arguments are filled (by name via --arg syntax)
        var filledByName = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Count positional values provided (not via named syntax)
        int positionalValuesProvided = 0;
        bool skipNextAsValue = false;
        string lastNamedArg = null;

        foreach (var part in inputParts)
        {
            if (skipNextAsValue)
            {
                skipNextAsValue = false;
                // The previous part was a named arg that takes a value
                if (lastNamedArg != null)
                    filledByName.Add(lastNamedArg);
                lastNamedArg = null;
                continue;
            }

            if (part.StartsWith("-"))
            {
                var argName = part.TrimStart('-');
                var argInfo = command.Arguments.FirstOrDefault(a =>
                    string.Equals(a.Name, argName, StringComparison.OrdinalIgnoreCase) ||
                    (argName.Length == 1 && a.Alias == argName[0]));

                if (argInfo != null)
                {
                    if (argInfo.IsOption)
                    {
                        // Boolean flag - doesn't take a value
                        filledByName.Add(argInfo.Name);
                    }
                    else
                    {
                        // Named argument that takes a value - skip next part
                        skipNextAsValue = true;
                        lastNamedArg = argInfo.Name;
                    }
                }
                continue;
            }

            // This is a positional value
            positionalValuesProvided++;
        }

        // Check for IsRest - can always accept more
        var hasRestPositional = positionalArgs.Any(a => a.IsRest);
        if (hasRestPositional)
            return true;

        // Find first unfilled positional slot
        int positionalIndex = 0;
        foreach (var posArg in positionalArgs)
        {
            // Skip if filled by name
            if (filledByName.Contains(posArg.Name))
                continue;

            // Check if this slot is filled by positional value
            if (positionalIndex < positionalValuesProvided)
            {
                positionalIndex++;
                continue;
            }

            // Found an unfilled slot
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the current positional slot index for the command.
    /// Takes into account positional arguments filled via named syntax (e.g., --Source).
    /// </summary>
    /// <param name="commandName">The command name.</param>
    /// <param name="inputParts">The parts of the input after the command.</param>
    /// <returns>The positional slot index, or -1 if all slots are filled.</returns>
    private int GetCurrentPositionalSlot(string commandName, string[] inputParts)
    {
        var command = GetCommand(commandName);
        if (command == null)
            return -1;

        var positionalArgs = command.Arguments
            .Where(a => a.IsPositional)
            .OrderBy(a => a.Position)
            .ToList();

        if (!positionalArgs.Any())
            return -1;

        // Track which positional arguments are filled (by name via --arg syntax)
        var filledByName = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Count positional values provided (not via named syntax)
        int positionalValuesProvided = 0;
        bool skipNextAsValue = false;
        string lastNamedArg = null;

        foreach (var part in inputParts)
        {
            if (skipNextAsValue)
            {
                skipNextAsValue = false;
                if (lastNamedArg != null)
                    filledByName.Add(lastNamedArg);
                lastNamedArg = null;
                continue;
            }

            if (part.StartsWith("-"))
            {
                var argName = part.TrimStart('-');
                var argInfo = command.Arguments.FirstOrDefault(a =>
                    string.Equals(a.Name, argName, StringComparison.OrdinalIgnoreCase) ||
                    (argName.Length == 1 && a.Alias == argName[0]));

                if (argInfo != null)
                {
                    if (argInfo.IsOption)
                    {
                        filledByName.Add(argInfo.Name);
                    }
                    else
                    {
                        skipNextAsValue = true;
                        lastNamedArg = argInfo.Name;
                    }
                }
                continue;
            }

            positionalValuesProvided++;
        }

        // Find first unfilled positional slot
        int positionalIndex = 0;
        foreach (var posArg in positionalArgs)
        {
            // Skip if filled by name
            if (filledByName.Contains(posArg.Name))
                continue;

            // IsRest always accepts more values - return immediately
            if (posArg.IsRest)
                return posArg.Position;

            // Check if this slot is filled by positional value
            if (positionalIndex < positionalValuesProvided)
            {
                positionalIndex++;
                continue;
            }

            // Found an unfilled slot
            return posArg.Position;
        }

        return -1;
    }

    /// <summary>
    /// Gets the names of positional arguments that have been filled by positional values.
    /// This is used to add them to UsedArguments so they don't appear in argument name completion.
    /// </summary>
    /// <param name="commandName">The command name.</param>
    /// <param name="inputBuffer">The full input buffer.</param>
    /// <param name="cursorPosition">The cursor position.</param>
    /// <returns>The names of positional arguments that are filled.</returns>
    private HashSet<string> GetFilledPositionalArgumentNames(string commandName, string inputBuffer, int cursorPosition)
    {
        var filledArgs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        var command = GetCommand(commandName);
        if (command == null)
            return filledArgs;

        var positionalArgs = command.Arguments
            .Where(a => a.IsPositional)
            .OrderBy(a => a.Position)
            .ToList();

        if (!positionalArgs.Any())
            return filledArgs;

        // Parse the input to get argument parts
        var textBeforeCursor = inputBuffer?.Substring(0, Math.Min(cursorPosition, inputBuffer?.Length ?? 0)) ?? string.Empty;
        var allParts = textBeforeCursor.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Find command parts to skip
        var commandParts = commandName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var inputParts = allParts.Skip(commandParts.Length).ToArray();

        // If we're at a space after the last part, don't exclude the current token
        var endsWithSpace = textBeforeCursor.EndsWith(" ");
        
        // Track which positional arguments are filled by name (--arg syntax)
        var filledByName = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Collect positional values (values not prefixed with -)
        var positionalValues = new List<string>();
        bool skipNextAsValue = false;
        string lastNamedArg = null;

        for (int i = 0; i < inputParts.Length; i++)
        {
            var part = inputParts[i];
            var isCurrentToken = !endsWithSpace && i == inputParts.Length - 1;

            if (skipNextAsValue)
            {
                skipNextAsValue = false;
                if (lastNamedArg != null)
                    filledByName.Add(lastNamedArg);
                lastNamedArg = null;
                continue;
            }

            if (part.StartsWith("-"))
            {
                var argName = part.TrimStart('-');
                var argInfo = command.Arguments.FirstOrDefault(a =>
                    string.Equals(a.Name, argName, StringComparison.OrdinalIgnoreCase) ||
                    (argName.Length == 1 && a.Alias == argName[0]));

                if (argInfo != null)
                {
                    if (argInfo.IsOption)
                    {
                        filledByName.Add(argInfo.Name);
                    }
                    else
                    {
                        skipNextAsValue = true;
                        lastNamedArg = argInfo.Name;
                    }
                }
                continue;
            }

            // This is a positional value
            // Only count it as filling a slot if it's not the current token being typed
            // (unless cursor is after a space, in which case all tokens are complete)
            if (!isCurrentToken)
            {
                positionalValues.Add(part);
            }
        }

        // Now match positional values to positional arguments
        int valueIndex = 0;
        foreach (var posArg in positionalArgs)
        {
            // Skip if this arg was filled by name
            if (filledByName.Contains(posArg.Name))
            {
                filledArgs.Add(posArg.Name);
                continue;
            }

            // IsRest consumes remaining values
            if (posArg.IsRest)
            {
                if (valueIndex < positionalValues.Count)
                {
                    filledArgs.Add(posArg.Name);
                }
                break;
            }

            // Check if we have a value for this positional slot
            if (valueIndex < positionalValues.Count)
            {
                filledArgs.Add(posArg.Name);
                valueIndex++;
            }
        }

        return filledArgs;
    }

    /// <summary>
    /// Gets completions from the first matching provider.
    /// </summary>
    private async Task<CompletionResult> GetCompletionsFromProvidersAsync(
        CompletionContext context,
        CancellationToken cancellationToken)
    {
        foreach (var provider in _providers)
        {
            if (cancellationToken.IsCancellationRequested)
                return CompletionResult.Empty;

            if (provider.CanHandle(context))
            {
                try
                {
                    var result = await provider.GetCompletionsAsync(context, cancellationToken);
                    if (result.Items.Count > 0)
                        return result;
                }
                catch (OperationCanceledException)
                {
                    return CompletionResult.Empty;
                }
                catch (Exception ex)
                {
                    return CompletionResult.Error(ex.Message);
                }
            }
        }

        return CompletionResult.Empty;
    }

    /// <summary>
    /// Moves the selection in the menu by the specified delta.
    /// </summary>
    private CompletionAction MoveSelection(int delta)
    {
        if (_menuState == null || _filteredItems.Count == 0)
            return CompletionAction.None();

        var newIndex = _menuState.SelectedIndex + delta;

        // Wrap around
        if (newIndex < 0)
            newIndex = _filteredItems.Count - 1;
        else if (newIndex >= _filteredItems.Count)
            newIndex = 0;

        // Adjust viewport if needed
        var viewportStart = _menuState.ViewportStart;
        var viewportEnd = viewportStart + _menuState.ViewportSize - 1;

        if (newIndex < viewportStart)
            viewportStart = newIndex;
        else if (newIndex > viewportEnd)
            viewportStart = newIndex - _menuState.ViewportSize + 1;

        _menuState = new MenuState
        {
            Items = _filteredItems,
            SelectedIndex = newIndex,
            ViewportStart = viewportStart,
            ViewportSize = _menuState.ViewportSize,
            TotalCount = _menuState.TotalCount
        };

        return CompletionAction.UpdateMenu(_menuState);
    }

    /// <summary>
    /// Closes the completion menu and resets state.
    /// </summary>
    private void CloseMenu()
    {
        _menuState = null;
        _allItems.Clear();
        _filteredItems.Clear();
        _currentQuery = string.Empty;
    }
}
