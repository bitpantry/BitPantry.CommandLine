using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BitPantry.CommandLine.Processing.Parsing
{
    //TODO: Need to validate for invalid characters in command names, arguments, and aliases

    /// <summary>
    /// Parses an input string and provides information regarding the various elements that make up the input string, including
    /// parsed representations of each element, context within and between elements, and any validation errors collected during 
    /// parsing
    /// </summary>    
    public class ParsedInput
    {
        /// <summary>
        /// The amount of white space at the front of the input string
        /// </summary>
        public int LeadingWhiteSpaceCount { get; set; }

        /// <summary>
        /// The list of parsed elements
        /// </summary>
        public IReadOnlyList<ParsedInputElement> Elements { get; set; }

        /// <summary>
        /// Any validation errors accumulated during the parsing of the input string
        /// </summary>
        public IReadOnlyCollection<ParsedInputValidationError> Errors
        {
            get
            {
                var errors = new List<ParsedInputValidationError>();

                // add error for missing command element

                if (GetCommandElement() == null)
                    errors.Add(new ParsedInputValidationError
                    {
                        Type = ParsedInputValidationErrorType.NoCommandElement
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
            && !Elements.Any(n => n.ElementType == InputElementType.Unexpected);

        /// <summary>
        /// Creates a new instance of the ParsedInput class using the given input string
        /// </summary>
        /// <param name="input">The input string to parse</param>
        public ParsedInput(string input)
        {
            // identify leading white space and cleanup front

            LeadingWhiteSpaceCount = input.Length - input.TrimStart().Length;
            input = input.TrimStart();

            // create input elements list

            var elems = new List<ParsedInputElement>();

            // split input and parse elements

            var splitInput = SplitInput(input);
            for (int i = 0; i < splitInput.Count; i++)
            {
                // add up length of previous element

                int locationStart = LeadingWhiteSpaceCount
                    + elems.Select(n => n.EndPosition + 1 - n.StartPosition).Sum() + 1;

                // parse next element

                elems.Add(new ParsedInputElement(splitInput[i]
                    , i
                    , locationStart
                    , locationStart + splitInput[i].Length - 1 
                    , elems.LastOrDefault(n => n.ElementType != InputElementType.Empty)));
            }

            // convert elements list to readonly

            Elements = elems.AsReadOnly();
        }

        /// <summary>
        /// Splits the given string while preserving the content of quoted values
        /// </summary>
        /// <param name="input">The input string to split</param>
        /// <returns>A list of individual raw elements</returns>
        private List<string> SplitInput(string input)
        {
            char delimiter = ' ';

            Regex csvPreservingQuotedStrings = new Regex(string.Format("(\"[^\"]*\"|[^{0}])+|(\\s?)+", delimiter));
            var values =
                 csvPreservingQuotedStrings.Matches(input)
                .Cast<Match>()
                .Where(m => !string.IsNullOrEmpty(m.Value))
                .Select(m => m.Value);
            return values.ToList();
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
        public ParsedInputElement GetElementAtPosition(int position)
        {
            return Elements.Where(n => n.StartPosition <= position && n.EndPosition >= position).FirstOrDefault();
        }

        /// <summary>
        /// Returns the parsed input element with element type of Command
        /// </summary>
        /// <returns>The command element</returns>
        public ParsedInputElement GetCommandElement()
        {
            return Elements.FirstOrDefault(n => n.ElementType == InputElementType.Command);
        }

    }
}
