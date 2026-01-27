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
            var cursorPosition = ComputeCursorPosition(input, line.BufferPosition);

            _lastContext = _contextResolver.Resolve(input, cursorPosition);
            var suggestion = GetSuggestionForContext(_lastContext, input);

            if (suggestion != null)
            {
                _ghostTextController.Show(suggestion, line);
            }
            else
            {
                _ghostTextController.Hide(line);
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
        /// </summary>
        /// <param name="line">The current input line.</param>
        public void Dismiss(ConsoleLineMirror line)
        {
            _ghostTextController.Hide(line);
            _lastContext = null;
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

        private string GetSuggestionForContext(CursorContext context, string input)
        {
            switch (context.ContextType)
            {
                case CursorContextType.GroupOrCommand:
                case CursorContextType.CommandOrSubgroupInGroup:
                    return GetCommandSyntaxSuggestion(context, input);

                case CursorContextType.ArgumentName:
                    return GetArgumentNameSuggestion(context, input);

                case CursorContextType.ArgumentAlias:
                    return GetArgumentAliasSuggestion(context, input);

                case CursorContextType.ArgumentValue:
                    return GetArgumentValueSuggestion(context, input);

                case CursorContextType.PositionalValue:
                    return GetPositionalValueSuggestion(context, input);

                case CursorContextType.Empty:
                default:
                    return null;
            }
        }

        private string GetCommandSyntaxSuggestion(CursorContext context, string input)
        {
            var query = context.QueryText ?? "";
            
            // Build context for the handler
            var handlerContext = CreateHandlerContext(context, input);
            
            // Get options from the command syntax handler
            var options = _commandSyntaxHandler.GetOptionsAsync(handlerContext).GetAwaiter().GetResult();
            
            return GetBestCompletionFromOptions(options, query);
        }

        private string GetArgumentNameSuggestion(CursorContext context, string input)
        {
            if (context.ResolvedCommand == null)
                return null;
                
            var query = context.QueryText ?? "";
            
            // Build context for the handler - argument name handler expects "--" prefix in query
            var handlerContext = CreateHandlerContext(context, input, queryPrefix: "--");
            
            // Get options from the argument name handler
            var options = _argumentNameHandler.GetOptionsAsync(handlerContext).GetAwaiter().GetResult();
            
            // Options come back as "--argname", we need to strip the prefix and return completion
            return GetBestCompletionFromOptions(options, "--" + query);
        }

        private string GetArgumentAliasSuggestion(CursorContext context, string input)
        {
            if (context.ResolvedCommand == null)
                return null;
                
            var query = context.QueryText ?? "";
            
            // Build context for the handler - argument alias handler expects "-" prefix in query
            var handlerContext = CreateHandlerContext(context, input, queryPrefix: "-");
            
            // Get options from the argument alias handler
            var options = _argumentAliasHandler.GetOptionsAsync(handlerContext).GetAwaiter().GetResult();
            
            // Options come back as "-a", we need to strip the prefix and return completion
            return GetBestCompletionFromOptions(options, "-" + query);
        }

        private string GetArgumentValueSuggestion(CursorContext context, string input)
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
                return GetBestCompletionFromOptions(options, query);
            }
        }

        private string GetPositionalValueSuggestion(CursorContext context, string input)
        {
            // Positional arguments use the same TargetArgument property as named arguments
            // The CursorContextResolver populates TargetArgument with the positional argument
            // based on the PositionalIndex
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
                return GetBestCompletionFromOptions(options, query);
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

        private string GetBestCompletionFromOptions(List<AutoCompleteOption> options, string query)
        {
            if (options == null || options.Count == 0)
                return null;

            // Options are already sorted alphabetically by the handlers
            // Find the first one that matches the query
            var match = options
                .Where(o => o.Value.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (match == null)
                return null;

            // If query exactly matches, no suggestion needed
            if (match.Value.Equals(query, StringComparison.OrdinalIgnoreCase))
                return null;

            // Return only the completion part (what remains after the query)
            return match.Value.Substring(query.Length);
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
