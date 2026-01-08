using System;

namespace BitPantry.VirtualConsole.AnsiParser
{
    /// <summary>
    /// Represents a parsed CSI (Control Sequence Introducer) sequence.
    /// </summary>
    public class CsiSequence
    {
        /// <summary>
        /// The numeric parameters of the sequence.
        /// </summary>
        public int[] Parameters { get; }

        /// <summary>
        /// The final character that identifies the command.
        /// </summary>
        public char FinalByte { get; }

        /// <summary>
        /// Whether this is a private sequence (starts with '?').
        /// </summary>
        public bool IsPrivate { get; }

        /// <summary>
        /// Creates a new CSI sequence.
        /// </summary>
        public CsiSequence(int[] parameters, char finalByte, bool isPrivate = false)
        {
            Parameters = parameters ?? Array.Empty<int>();
            FinalByte = finalByte;
            IsPrivate = isPrivate;
        }

        /// <summary>
        /// Gets a parameter value, or a default if not present.
        /// </summary>
        /// <param name="index">Parameter index (0-based).</param>
        /// <param name="defaultValue">Default value if parameter is missing or zero.</param>
        /// <returns>The parameter value or default.</returns>
        public int GetParameter(int index, int defaultValue = 1)
        {
            if (index >= 0 && index < Parameters.Length && Parameters[index] != 0)
            {
                return Parameters[index];
            }
            return defaultValue;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var paramsStr = Parameters.Length > 0 ? string.Join(";", Parameters) : "";
            return $"CSI {(IsPrivate ? "? " : "")}{paramsStr} {FinalByte}";
        }
    }
}
