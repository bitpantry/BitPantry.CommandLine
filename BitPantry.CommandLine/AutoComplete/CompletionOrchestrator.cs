using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.Component;

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
        CommandRegistry registry)
    {
        _providers = providers?.OrderByDescending(p => p.Priority) 
            ?? throw new ArgumentNullException(nameof(providers));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
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

        // Try cache first
        var cacheKey = new CacheKey(context.CommandName, context.ArgumentName, context.PartialValue);
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
                
                // Only return ghost if the match is a prefix match
                if (bestMatch.InsertText.StartsWith(inputBuffer, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(bestMatch.InsertText, inputBuffer, StringComparison.OrdinalIgnoreCase))
                {
                    // Return just the ghost part (suffix after input)
                    return bestMatch.InsertText.Substring(inputBuffer.Length);
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
            IsRemote = isRemote
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

        var lastPart = parts[^1];

        // Check for argument name completion (starts with --)
        if (lastPart.StartsWith("--"))
        {
            var commandName = FindCommandName(parts.Take(parts.Length - 1).ToArray());
            return (CompletionElementType.ArgumentName, commandName, null, lastPart.Substring(2));
        }

        // Check for argument alias completion (starts with -)
        if (lastPart.StartsWith("-") && !lastPart.StartsWith("--"))
        {
            var commandName = FindCommandName(parts.Take(parts.Length - 1).ToArray());
            return (CompletionElementType.ArgumentAlias, commandName, null, lastPart.Substring(1));
        }

        // Check if previous part is an argument name (expecting value)
        if (parts.Length >= 2)
        {
            var prevPart = parts[^2];
            if (prevPart.StartsWith("--") || prevPart.StartsWith("-"))
            {
                var commandName = FindCommandName(parts.Take(parts.Length - 2).ToArray());
                var argName = prevPart.TrimStart('-');
                return (CompletionElementType.ArgumentValue, commandName, argName, lastPart);
            }
        }

        // Default to command completion
        // Check if this might be within a group context
        var commandParts = new List<string>(parts);
        var partial = commandParts[^1];

        // Try to find if we're in a group context
        var groupPath = string.Empty;
        for (int i = 0; i < commandParts.Count - 1; i++)
        {
            var potentialGroup = string.IsNullOrEmpty(groupPath) 
                ? commandParts[i] 
                : $"{groupPath} {commandParts[i]}";
            
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
