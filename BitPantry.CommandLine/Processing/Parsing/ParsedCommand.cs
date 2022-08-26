using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BitPantry.CommandLine.Processing.Parsing
{
    /// <summary>
    /// Parses a string for a single command and provides information regarding the various elements that make up the command invocation 
    /// string, including parsed representations of each element, context within and between elements, and any validation errors collected 
    /// during parsing
    /// </summary>    
    public class ParsedCommand
    {
        /// <summary>
        /// The amount of white space at the front of the input string
        /// </summary>
        public int LeadingWhiteSpaceCount { get; set; }

        /// <summary>
        /// The list of parsed elements
        /// </summary>
        public IReadOnlyList<ParsedCommandElement> Elements { get; set; }

        /// <summary>
        /// Any validation errors accumulated during the parsing of the input string
        /// </summary>
        public IReadOnlyCollection<ParsedCommandValidationError> Errors
        {
            get
            {
                var errors = new List<ParsedCommandValidationError>();

                // add error for missing command element

                if (GetCommandElement() == null)
                    errors.Add(new ParsedCommandValidationError
                    {
                        Type = ParsedCommandValidationErrorType.NoCommandElement
                    });

                // pull in element errors

                foreach (var elem in Elements)
                    errors.AddRange(elem.ValidationErrors);

                return errors.AsReadOnly();
            }
        }


        /// <summary>
        /// Whether or not the input string is valid - has a command element; has no validation errors; has no unexpected elements
        /// </summary>
        public bool IsValid => 
            GetCommandElement() != null
            && !Errors.Any()
            && !Elements.Any(n => n.ElementType == CommandElementType.Unexpected);

        /// <summary>
        /// Creates a new instance of the ParsedCommandInput class using the given input string
        /// </summary>
        /// <param name="str">The input string to parse</param>
        public ParsedCommand(string str)
        {
            // identify leading white space and cleanup front

            LeadingWhiteSpaceCount = str.Length - str.TrimStart().Length;
            str = str.TrimStart();

            // create input elements list

            var elems = new List<ParsedCommandElement>();

            // split input and parse elements

            var splitInput = StringParsing.SplitCommandString(str);
            for (int i = 0; i < splitInput.Count; i++)
            {
                // add up length of previous element

                int locationStart = LeadingWhiteSpaceCount
                    + elems.Select(n => n.EndPosition + 1 - n.StartPosition).Sum() + 1;

                // parse next element

                elems.Add(new ParsedCommandElement(splitInput[i]
                    , i
                    , locationStart
                    , locationStart + splitInput[i].Length - 1 
                    , elems.LastOrDefault(n => n.ElementType != CommandElementType.Empty)));
            }

            // convert elements list to readonly

            Elements = elems.AsReadOnly();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(string.Empty.PadLeft(LeadingWhiteSpaceCount, ' '));
            foreach (var elem in Elements)
                sb.AppendFormat("{0}", elem.ToString());
            return sb.ToString();
        }

        /// <summary>
        /// Returns the element at a given character position in the input string
        /// </summary>
        /// <param name="position">The character position</param>
        /// <returns>A parsed input element</returns>
        public ParsedCommandElement GetElementAtPosition(int position)
        {
            return Elements.Where(n => n.StartPosition <= position && n.EndPosition >= position).FirstOrDefault();
        }

        /// <summary>
        /// Returns the parsed input element with element type of Command, including the full namespace if any
        /// </summary>
        /// <returns>The command element</returns>
        public ParsedCommandElement GetCommandElement()
        {
            return Elements.FirstOrDefault(n => n.ElementType == CommandElementType.Command);
        }

    }
}
