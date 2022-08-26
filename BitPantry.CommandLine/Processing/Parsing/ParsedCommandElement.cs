using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.Processing.Parsing
{
    /// <summary>
    /// The possible types of input elements
    /// </summary>
    public enum CommandElementType
    {
        /// <summary>
        /// The element represents the command to invoke - this is the full command name including namespace
        /// </summary>
        Command,

        /// <summary>
        /// The element represents an argument name
        /// </summary>
        ArgumentName,

        /// <summary>
        /// The element represents an argument alias
        /// </summary>
        ArgumentAlias,

        /// <summary>
        /// The element represents an argument value
        /// </summary>
        ArgumentValue,

        /// <summary>
        /// The element represents empty space in the input string
        /// </summary>
        Empty,

        /// <summary>
        /// Either due to location or syntax, the element represents an unexpected / unknown value
        /// </summary>
        Unexpected
    }

    /// <summary>
    /// Represents a parsed input element and its context in the input string and in relation to other elements
    /// </summary>
    public class ParsedCommandElement
    {
        private readonly string[] ValidPrefixes = new string[]
        {
            CommandInputParsingConstants.ElementPrefixArgument.ToString(),
            CommandInputParsingConstants.ElementPrefixAlias.ToString()
        };

        private readonly CommandElementType[] ArgumentElementTypes = new CommandElementType[]
        {
            CommandElementType.ArgumentName,
            CommandElementType.ArgumentAlias
        };

        /// <summary>
        /// The raw element - e.g., for an argument, "--paramOne", the Raw value would be "--paramOne" (as opposed to Value)
        /// </summary>
        public string Raw { get; private set; }

        /// <summary>
        /// The parsed value that is represented by the element - e.g., for an argument, "--paramOne", 
        /// the value would be "paramOne"
        /// </summary>
        public string Value => TrimElement();

        /// <summary>
        /// The element type as determined by the parser based on content, prefix, and location within the input string
        /// </summary>
        public CommandElementType ElementType { get; private set; }

        /// <summary>
        /// The index of the element in the list of other elements
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// the index of the first character of the element in the input string
        /// </summary>
        public int StartPosition { get; private set; }

        /// <summary>
        /// The index of the last character of the element in the input string
        /// </summary>
        public int EndPosition { get; private set; }

        /// <summary>
        /// Based on element type and the relative position of other elements in the input string, the element symantically
        /// paired to this element
        /// </summary>
        public ParsedCommandElement IsPairedWith { get; private set; }

        /// <summary>
        /// Any validation errors collected during the parsing of the input string
        /// </summary>
        public IReadOnlyCollection<ParsedCommandValidationError> ValidationErrors { get; private set; }

        /// <summary>
        /// Parses the given element and determines its context and relationship to other elements in the input string
        /// </summary>
        /// <param name="element">The raw element to parse</param>
        /// <param name="index">The index of the element in the list of other elements</param>
        /// <param name="locationStart">The index of the first character of the element in the input string</param>
        /// <param name="locationEnd">The index of the last character of the element in the input string</param>
        /// <param name="previousElement">The first non-empty element immediately preceeding this one</param>
        public ParsedCommandElement(
            string element, 
            int index, 
            int locationStart, 
            int locationEnd, 
            ParsedCommandElement previousElement)
        {
            Raw = element;
            Index = index;
            StartPosition = locationStart;
            EndPosition = locationEnd;

            if (element.Trim().StartsWith(CommandInputParsingConstants.ElementPrefixArgument.ToString()))
            {
                ElementType = string.IsNullOrEmpty(Value) ? CommandElementType.Unexpected : CommandElementType.ArgumentName;

            }
            else if (element.Trim().StartsWith(CommandInputParsingConstants.ElementPrefixAlias.ToString()))
            {
                ElementType = string.IsNullOrEmpty(Value) ? CommandElementType.Unexpected : CommandElementType.ArgumentAlias;
            }
            else // standard string input (concurrent or quoted) is an argument value
            {
                if (!string.IsNullOrWhiteSpace(Raw)
                    && previousElement != null
                    && ArgumentElementTypes.Contains(previousElement.ElementType)) // string value appearing right after argument
                {
                    ElementType = CommandElementType.ArgumentValue;
                    IsPairedWith = previousElement;
                    previousElement.IsPairedWith = this;
                }
                else if (previousElement == null) // string value appearing as the first element
                {
                    ElementType = CommandElementType.Command;
                }
                else if (!string.IsNullOrWhiteSpace(Raw)) // unexpected element
                {
                    ElementType = CommandElementType.Unexpected;
                }
                else // empty string
                {
                    ElementType = CommandElementType.Empty;
                }
            }

            Validate();
        }

        public override string ToString()
        {
            return Raw;
        }

        /// <summary>
        /// Trims the element to a value representation
        /// </summary>
        /// <returns></returns>
        private string TrimElement()
        {
            var elem = Raw.Trim().Trim('"');

            foreach (var prefix in ValidPrefixes)
            {
                if (elem.StartsWith(prefix))
                {
                    elem = elem.Substring(prefix.Length);
                    break;
                }
            }

            return elem;
        }

        private void Validate()
        {
            var errors = new List<ParsedCommandValidationError>();

            // adds an error for an argument alias that contains more than one character

            if (ElementType == CommandElementType.ArgumentAlias
                && Value.Length > 1)
                errors.Add(new ParsedCommandValidationError
                {
                    Type = ParsedCommandValidationErrorType.InvalidAlias,
                    Element = this,
                    Message = $"Alias, \"{Raw}\", is invalid. Aliases are expected to be one character in length."
                });

            

            ValidationErrors = errors.AsReadOnly();
        }

    }
}
