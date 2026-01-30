using System;
using System.Collections.Generic;
using System.Linq;
using BitPantry.CommandLine.AutoComplete.Context;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.AutoComplete.Syntax;
using BitPantry.CommandLine.Component;

namespace BitPantry.CommandLine.AutoComplete
{
    /// <summary>
    /// Provides autocomplete suggestions by resolving cursor context and filtering options.
    /// Handles all the logic for determining what suggestions are available.
    /// </summary>
    public class AutoCompleteSuggestionProvider
    {
        private readonly ICommandRegistry _registry;
        private readonly CommandSyntaxHandler _commandSyntaxHandler;
        private readonly ArgumentNameHandler _argumentNameHandler;
        private readonly ArgumentAliasHandler _argumentAliasHandler;
        private readonly IAutoCompleteHandlerRegistry _handlerRegistry;
        private readonly AutoCompleteHandlerActivator _handlerActivator;

        /// <summary>
        /// Creates a new AutoCompleteSuggestionProvider.
        /// </summary>
        public AutoCompleteSuggestionProvider(
            ICommandRegistry registry,
            IAutoCompleteHandlerRegistry handlerRegistry,
            AutoCompleteHandlerActivator handlerActivator)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _handlerRegistry = handlerRegistry ?? throw new ArgumentNullException(nameof(handlerRegistry));
            _handlerActivator = handlerActivator ?? throw new ArgumentNullException(nameof(handlerActivator));

            _commandSyntaxHandler = new CommandSyntaxHandler(registry);
            _argumentNameHandler = new ArgumentNameHandler();
            _argumentAliasHandler = new ArgumentAliasHandler();
        }

        /// <summary>
        /// Gets filtered autocomplete options for the given context.
        /// </summary>
        /// <param name="context">The cursor context.</param>
        /// <param name="input">The full input text.</param>
        /// <returns>List of matching options, or null if none available.</returns>
        public List<AutoCompleteOption> GetOptions(CursorContext context, string input)
        {
            if (context == null)
            {
                return null;
            }

            return context.ContextType switch
            {
                CursorContextType.GroupOrCommand => GetCommandSyntaxOptions(context, input),
                CursorContextType.CommandOrSubgroupInGroup => GetCommandSyntaxOptions(context, input),
                CursorContextType.ArgumentName => GetArgumentNameOptions(context, input),
                CursorContextType.ArgumentAlias => GetArgumentAliasOptions(context, input),
                CursorContextType.ArgumentValue => GetArgumentValueOptions(context, input),
                CursorContextType.PositionalValue => GetPositionalValueOptions(context, input),
                _ => null
            };
        }

        /// <summary>
        /// Gets the ghost text suggestion from the given options.
        /// Returns the completion portion of the first matching option.
        /// </summary>
        /// <param name="options">The available options.</param>
        /// <param name="context">The cursor context.</param>
        /// <returns>The ghost text to display, or null if none.</returns>
        public string GetGhostText(List<AutoCompleteOption> options, CursorContext context)
        {
            if (options == null || options.Count == 0 || context == null)
            {
                return null;
            }

            var firstOption = options[0];
            var optionValue = firstOption.Value;

            // Build the full query including any prefix
            var query = context.QueryText ?? "";
            var fullQuery = context.ContextType switch
            {
                CursorContextType.ArgumentName => "--" + query,
                CursorContextType.ArgumentAlias => "-" + query,
                _ => query
            };

            // If query exactly matches first option, no suggestion needed
            if (optionValue.Equals(fullQuery, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Check if this is a value context where quoting applies
            var isValueContext = context.ContextType == CursorContextType.ArgumentValue
                               || context.ContextType == CursorContextType.PositionalValue;

            if (isValueContext)
            {
                return GetQuotedValueSuggestion(optionValue, query, context);
            }

            // Return only the completion part
            if (optionValue.Length > fullQuery.Length)
            {
                return optionValue.Substring(fullQuery.Length);
            }

            return null;
        }

        /// <summary>
        /// Determines if a trailing space should be added after accepting a suggestion.
        /// </summary>
        /// <param name="context">The cursor context.</param>
        /// <returns>True if a trailing space should be added.</returns>
        public bool ShouldAddTrailingSpace(CursorContext context)
        {
            if (context == null)
            {
                return false;
            }

            return context.ContextType == CursorContextType.GroupOrCommand
                || context.ContextType == CursorContextType.CommandOrSubgroupInGroup
                || context.ContextType == CursorContextType.ArgumentName;
        }

        /// <summary>
        /// Determines if the cursor is within a quoted string context.
        /// </summary>
        /// <param name="context">The cursor context.</param>
        /// <returns>True if in quote context.</returns>
        public bool IsInQuoteContext(CursorContext context)
        {
            var activeElement = context?.ActiveElement;
            if (activeElement == null)
            {
                return false;
            }

            var raw = activeElement.Raw;
            return !string.IsNullOrEmpty(raw) && raw.TrimStart().StartsWith("\"");
        }

        /// <summary>
        /// Gets the prefix for the given context type (-- for argument names, - for aliases).
        /// </summary>
        public string GetContextPrefix(CursorContext context)
        {
            if (context == null)
            {
                return "";
            }

            return context.ContextType switch
            {
                CursorContextType.ArgumentName => "--",
                CursorContextType.ArgumentAlias => "-",
                _ => ""
            };
        }

        #region Private Methods

        private string GetQuotedValueSuggestion(string optionValue, string query, CursorContext context)
        {
            var valueHasSpaces = optionValue.Contains(' ');
            var isInQuoteContext = IsInQuoteContext(context);

            if (isInQuoteContext)
            {
                if (optionValue.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                {
                    var remainder = optionValue.Substring(query.Length);
                    return remainder + "\"";
                }
                return null;
            }

            if (valueHasSpaces)
            {
                if (optionValue.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                {
                    var remainder = optionValue.Substring(query.Length);
                    return remainder;
                }
                return null;
            }

            if (optionValue.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            {
                var remainder = optionValue.Substring(query.Length);
                return remainder.Length > 0 ? remainder : null;
            }

            return null;
        }

        private List<AutoCompleteOption> GetCommandSyntaxOptions(CursorContext context, string input)
        {
            var query = context.QueryText ?? "";
            var handlerContext = CreateHandlerContext(context, input);
            var options = _commandSyntaxHandler.GetOptionsAsync(handlerContext).GetAwaiter().GetResult();

            return options?
                .Where(o => o.Value.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<AutoCompleteOption>();
        }

        private List<AutoCompleteOption> GetArgumentNameOptions(CursorContext context, string input)
        {
            if (context.ResolvedCommand == null)
            {
                return null;
            }

            var query = context.QueryText ?? "";
            var handlerContext = CreateHandlerContext(context, input, queryPrefix: "--");
            var options = _argumentNameHandler.GetOptionsAsync(handlerContext).GetAwaiter().GetResult();

            return options?
                .Where(o => o.Value.StartsWith("--" + query, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<AutoCompleteOption>();
        }

        private List<AutoCompleteOption> GetArgumentAliasOptions(CursorContext context, string input)
        {
            if (context.ResolvedCommand == null)
            {
                return null;
            }

            var query = context.QueryText ?? "";
            var handlerContext = CreateHandlerContext(context, input, queryPrefix: "-");
            var options = _argumentAliasHandler.GetOptionsAsync(handlerContext).GetAwaiter().GetResult();

            return options?
                .Where(o => o.Value.StartsWith("-" + query, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<AutoCompleteOption>();
        }

        private List<AutoCompleteOption> GetArgumentValueOptions(CursorContext context, string input)
        {
            if (context.TargetArgument == null)
            {
                return null;
            }

            var handlerType = _handlerRegistry.FindHandler(context.TargetArgument, _handlerActivator);
            if (handlerType == null)
            {
                return null;
            }

            var query = context.QueryText ?? "";
            var handlerContext = CreateHandlerContext(context, input);

            using (var activation = _handlerActivator.Activate(handlerType))
            {
                var options = activation.Handler.GetOptionsAsync(handlerContext).GetAwaiter().GetResult();
                return options?
                    .Where(o => o.Value.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    .ToList() ?? new List<AutoCompleteOption>();
            }
        }

        private List<AutoCompleteOption> GetPositionalValueOptions(CursorContext context, string input)
        {
            if (context.TargetArgument == null)
            {
                return null;
            }

            var handlerType = _handlerRegistry.FindHandler(context.TargetArgument, _handlerActivator);
            if (handlerType == null)
            {
                return null;
            }

            var query = context.QueryText ?? "";
            var handlerContext = CreateHandlerContext(context, input);

            using (var activation = _handlerActivator.Activate(handlerType))
            {
                var options = activation.Handler.GetOptionsAsync(handlerContext).GetAwaiter().GetResult();
                return options?
                    .Where(o => o.Value.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    .ToList() ?? new List<AutoCompleteOption>();
            }
        }

        private AutoCompleteContext CreateHandlerContext(CursorContext context, string input, string queryPrefix = "")
        {
            var query = queryPrefix + (context.QueryText ?? "");

            return new AutoCompleteContext
            {
                QueryString = query,
                FullInput = input,
                CursorPosition = context.CursorPosition,
                ArgumentInfo = context.TargetArgument,
                ProvidedValues = new Dictionary<ArgumentInfo, string>(),
                CommandInfo = context.ResolvedCommand
            };
        }

        #endregion
    }
}
