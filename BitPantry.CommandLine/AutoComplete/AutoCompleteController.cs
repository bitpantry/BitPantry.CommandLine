using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete.Context;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.AutoComplete.Syntax;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using Spectre.Console;

namespace BitPantry.CommandLine.AutoComplete
{
    /// <summary>
    /// The current mode of the autocomplete controller.
    /// </summary>
    public enum AutoCompleteMode
    {
        /// <summary>No autocomplete is active.</summary>
        Idle,
        /// <summary>Inline ghost text suggestion is shown.</summary>
        GhostText,
        /// <summary>Dropdown menu with multiple options is shown (future).</summary>
        Menu
    }

    /// <summary>
    /// Orchestrates autocomplete functionality by composing CursorContextResolver,
    /// syntax handlers, and GhostTextController to provide intelligent suggestions.
    /// </summary>
    public class AutoCompleteController
    {
        private readonly ICommandRegistry _registry;
        private readonly CursorContextResolver _contextResolver;
        private readonly GhostTextController _ghostTextController;
        private readonly CommandSyntaxHandler _commandSyntaxHandler;
        private readonly ArgumentNameHandler _argumentNameHandler;
        private readonly ArgumentAliasHandler _argumentAliasHandler;
        private readonly IAutoCompleteHandlerRegistry _handlerRegistry;
        private readonly AutoCompleteHandlerActivator _handlerActivator;
        private CursorContext _lastContext;

        /// <summary>
        /// Tracks the start position of the element where ghost text was suppressed via Escape.
        /// When set, ghost text will not appear while the cursor remains in that element.
        /// </summary>
        private int? _suppressedElementStart;

        /// <summary>
        /// Caches the available options from the last Update() call for Tab key logic.
        /// </summary>
        private List<AutoCompleteOption> _lastOptions;

        /// <summary>
        /// Gets the current autocomplete mode.
        /// </summary>
        public AutoCompleteMode Mode => _ghostTextController.IsShowing ? AutoCompleteMode.GhostText : AutoCompleteMode.Idle;

        /// <summary>
        /// Gets whether autocomplete is currently active (not Idle).
        /// </summary>
        public bool IsActive => Mode != AutoCompleteMode.Idle;

        /// <summary>
        /// Gets the current ghost text, or null if not in GhostText mode.
        /// </summary>
        public string GhostText => _ghostTextController.Text;

        /// <summary>
        /// Gets the count of available autocomplete options from the last Update() call.
        /// Returns 0 if no options, 1 if single option, >1 if multiple options.
        /// </summary>
        public int AvailableOptionCount => _lastOptions?.Count ?? 0;

        /// <summary>
        /// Resets the autocomplete controller state for a new input session.
        /// Clears any active suppression and ghost text.
        /// </summary>
        public void Reset()
        {
            _suppressedElementStart = null;
            _lastContext = null;
            _lastOptions = null;
            _ghostTextController.Clear();
        }

        /// <summary>
        /// Creates a new AutoCompleteController.
        /// </summary>
        /// <param name="registry">The command registry to resolve commands from.</param>
        /// <param name="console">The console to render ghost text to.</param>
        /// <param name="handlerRegistry">The handler registry for argument value suggestions.</param>
        /// <param name="handlerActivator">The activator to create handler instances.</param>
        public AutoCompleteController(
            ICommandRegistry registry, 
            IAnsiConsole console,
            IAutoCompleteHandlerRegistry handlerRegistry,
            AutoCompleteHandlerActivator handlerActivator)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            if (console == null) throw new ArgumentNullException(nameof(console));
            _handlerRegistry = handlerRegistry ?? throw new ArgumentNullException(nameof(handlerRegistry));
            _handlerActivator = handlerActivator ?? throw new ArgumentNullException(nameof(handlerActivator));

            _contextResolver = new CursorContextResolver(registry);
            _ghostTextController = new GhostTextController(console);
            _commandSyntaxHandler = new CommandSyntaxHandler(registry);
            _argumentNameHandler = new ArgumentNameHandler();
            _argumentAliasHandler = new ArgumentAliasHandler();
        }

        /// <summary>
        /// Updates the autocomplete display based on the current line content and cursor position.
        /// Computes the appropriate suggestion and renders ghost text if applicable.
        /// </summary>
        /// <param name="line">The current input line.</param>
        public void Update(ConsoleLineMirror line)
        {
            var input = line.Buffer;

            // Don't show ghost text on empty input
            if (string.IsNullOrEmpty(input))
            {
                _lastContext = null;
                _lastOptions = null;
                _ghostTextController.Clear();
                return;
            }

            var cursorPosition = ComputeCursorPosition(input, line.BufferPosition);

            _lastContext = _contextResolver.Resolve(input, cursorPosition);

            // Check if we've moved to a new element (clears suppression)
            if (_suppressedElementStart.HasValue && _lastContext.ReplacementStart != _suppressedElementStart.Value)
            {
                _suppressedElementStart = null;
            }

            // If suppressed in current element, don't show ghost text
            if (_suppressedElementStart.HasValue)
            {
                _lastOptions = null;
                _ghostTextController.Clear();
                return;
            }

            _lastOptions = GetFilteredOptions(_lastContext, input);

            // Ghost text is the first filtered option (if any)
            var suggestion = GetSuggestionFromOptions(_lastOptions, _lastContext);
            if (suggestion != null)
            {
                _ghostTextController.Show(suggestion, line);
            }
            else
            {
                _ghostTextController.Clear();
            }
        }

        /// <summary>
        /// Accepts the current suggestion, committing it to the line buffer.
        /// </summary>
        /// <param name="line">The current input line.</param>
        public void Accept(ConsoleLineMirror line)
        {
            if (!IsActive)
                return;

            // Determine if we should add a trailing space
            var shouldAddSpace = ShouldAddTrailingSpace(_lastContext);

            _ghostTextController.Accept(line);

            if (shouldAddSpace)
            {
                line.Write(" ");
            }
        }

        /// <summary>
        /// Dismisses the current autocomplete, returning to Idle mode.
        /// Ghost text may reappear on next Update if still relevant.
        /// </summary>
        /// <param name="line">The current input line.</param>
        public void Dismiss(ConsoleLineMirror line)
        {
            _ghostTextController.Clear();
            _lastOptions = null;
        }

        /// <summary>
        /// Suppresses ghost text for the current element.
        /// Ghost text won't reappear until cursor moves to a new element.
        /// </summary>
        /// <param name="line">The current input line.</param>
        public void Suppress(ConsoleLineMirror line)
        {
            // Record the current element's start position for suppression
            if (_lastContext != null)
            {
                _suppressedElementStart = _lastContext.ReplacementStart;
            }

            // Dismiss clears the ghost text
            Dismiss(line);
        }

        /// <summary>
        /// Converts from 0-based BufferPosition to 1-based cursorPosition for CursorContextResolver.
        /// </summary>
        private int ComputeCursorPosition(string input, int bufferPosition)
        {
            // For empty input, position is 1. For input ending with space (cursor in empty slot), 
            // position is BufferPosition + 1. For partial tokens, position equals BufferPosition.
            if (string.IsNullOrEmpty(input))
            {
                return 1;
            }
            else if (input.EndsWith(" ") || bufferPosition == 0)
            {
                return bufferPosition + 1;
            }
            else
            {
                return bufferPosition;
            }
        }

        private List<AutoCompleteOption> GetFilteredOptions(CursorContext context, string input)
        {
            switch (context.ContextType)
            {
                case CursorContextType.GroupOrCommand:
                case CursorContextType.CommandOrSubgroupInGroup:
                    return GetCommandSyntaxOptions(context, input);

                case CursorContextType.ArgumentName:
                    return GetArgumentNameOptions(context, input);

                case CursorContextType.ArgumentAlias:
                    return GetArgumentAliasOptions(context, input);

                case CursorContextType.ArgumentValue:
                    return GetArgumentValueOptions(context, input);

                case CursorContextType.PositionalValue:
                    return GetPositionalValueOptions(context, input);

                case CursorContextType.Empty:
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the ghost text suggestion from filtered options.
        /// Returns the completion portion of the first matching option, or null if none.
        /// </summary>
        private string GetSuggestionFromOptions(List<AutoCompleteOption> options, CursorContext context)
        {
            if (options == null || options.Count == 0)
                return null;

            var firstOption = options[0];
            var optionValue = firstOption.Value;

            // Build the full query including any prefix (-- for argument names, - for aliases)
            var query = context.QueryText ?? "";
            var fullQuery = context.ContextType switch
            {
                CursorContextType.ArgumentName => "--" + query,
                CursorContextType.ArgumentAlias => "-" + query,
                _ => query
            };

            // If query exactly matches first option, no suggestion needed
            if (optionValue.Equals(fullQuery, StringComparison.OrdinalIgnoreCase))
                return null;

            // Return only the completion part (what remains after the full query)
            if (optionValue.Length > fullQuery.Length)
                return optionValue.Substring(fullQuery.Length);

            return null;
        }

        private List<AutoCompleteOption> GetCommandSyntaxOptions(CursorContext context, string input)
        {
            var query = context.QueryText ?? "";
            
            // Build context for the handler
            var handlerContext = CreateHandlerContext(context, input);
            
            // Get options from the command syntax handler
            var options = _commandSyntaxHandler.GetOptionsAsync(handlerContext).GetAwaiter().GetResult();
            
            // Filter options by query
            return options?
                .Where(o => o.Value.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<AutoCompleteOption>();
        }

        private List<AutoCompleteOption> GetArgumentNameOptions(CursorContext context, string input)
        {
            if (context.ResolvedCommand == null)
                return null;
                
            var query = context.QueryText ?? "";
            
            // Build context for the handler - argument name handler expects "--" prefix in query
            var handlerContext = CreateHandlerContext(context, input, queryPrefix: "--");
            
            // Get options from the argument name handler
            var options = _argumentNameHandler.GetOptionsAsync(handlerContext).GetAwaiter().GetResult();
            
            // Filter options by query (with -- prefix)
            return options?
                .Where(o => o.Value.StartsWith("--" + query, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<AutoCompleteOption>();
        }

        private List<AutoCompleteOption> GetArgumentAliasOptions(CursorContext context, string input)
        {
            if (context.ResolvedCommand == null)
                return null;
                
            var query = context.QueryText ?? "";
            
            // Build context for the handler - argument alias handler expects "-" prefix in query
            var handlerContext = CreateHandlerContext(context, input, queryPrefix: "-");
            
            // Get options from the argument alias handler
            var options = _argumentAliasHandler.GetOptionsAsync(handlerContext).GetAwaiter().GetResult();
            
            // Filter options by query (with - prefix)
            return options?
                .Where(o => o.Value.StartsWith("-" + query, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<AutoCompleteOption>();
        }

        private List<AutoCompleteOption> GetArgumentValueOptions(CursorContext context, string input)
        {
            if (context.TargetArgument == null)
                return null;

            // Find a handler for this argument's type
            var handlerType = _handlerRegistry.FindHandler(context.TargetArgument, _handlerActivator);
            if (handlerType == null)
                return null;

            var query = context.QueryText ?? "";

            // Build context for the handler
            var handlerContext = CreateHandlerContext(context, input);

            // Activate the handler and get options
            using (var activation = _handlerActivator.Activate(handlerType))
            {
                var options = activation.Handler.GetOptionsAsync(handlerContext).GetAwaiter().GetResult();
                
                // Filter options by query
                return options?
                    .Where(o => o.Value.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    .ToList() ?? new List<AutoCompleteOption>();
            }
        }

        private List<AutoCompleteOption> GetPositionalValueOptions(CursorContext context, string input)
        {
            // Positional arguments use the same TargetArgument property as named arguments
            if (context.TargetArgument == null)
                return null;

            // Find a handler for this argument's type (same logic as named arguments)
            var handlerType = _handlerRegistry.FindHandler(context.TargetArgument, _handlerActivator);
            if (handlerType == null)
                return null;

            var query = context.QueryText ?? "";

            // Build context for the handler
            var handlerContext = CreateHandlerContext(context, input);

            // Activate the handler and get options
            using (var activation = _handlerActivator.Activate(handlerType))
            {
                var options = activation.Handler.GetOptionsAsync(handlerContext).GetAwaiter().GetResult();
                
                // Filter options by query
                return options?
                    .Where(o => o.Value.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    .ToList() ?? new List<AutoCompleteOption>();
            }
        }

        private AutoCompleteContext CreateHandlerContext(CursorContext context, string input, string queryPrefix = "")
        {
            var query = queryPrefix + (context.QueryText ?? "");
            
            // Get argument and command info from context
            var argumentInfo = context.TargetArgument;
            var commandInfo = context.ResolvedCommand;
            
            // For syntax contexts, we may not have command/argument info yet
            // The handlers are designed to handle null CommandInfo gracefully
            return new AutoCompleteContext
            {
                QueryString = query,
                FullInput = input,
                CursorPosition = context.CursorPosition,
                ArgumentInfo = argumentInfo,
                ProvidedValues = new Dictionary<ArgumentInfo, string>(),
                CommandInfo = commandInfo
            };
        }

        private bool ShouldAddTrailingSpace(CursorContext context)
        {
            if (context == null)
                return false;

            // Add space after completing groups, commands, or argument names
            return context.ContextType == CursorContextType.GroupOrCommand
                || context.ContextType == CursorContextType.CommandOrSubgroupInGroup
                || context.ContextType == CursorContextType.ArgumentName;
        }
    }
}
