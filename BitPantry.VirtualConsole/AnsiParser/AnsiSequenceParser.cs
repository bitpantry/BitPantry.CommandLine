using System;
using System.Collections.Generic;

namespace BitPantry.VirtualConsole.AnsiParser
{
    /// <summary>
    /// State machine parser for ANSI escape sequences.
    /// </summary>
    public class AnsiSequenceParser
    {
        private enum State
        {
            Ground,
            Escape,
            CsiEntry,
            CsiParam
        }

        private State _state = State.Ground;
        private readonly List<int> _parameters = new List<int>();
        private int _currentParam = 0;
        private bool _hasCurrentParam = false;
        private bool _isPrivate = false;

        /// <summary>
        /// Processes a single character and returns the result.
        /// </summary>
        /// <param name="c">The character to process.</param>
        /// <returns>The result of processing the character.</returns>
        public ParserResult Process(char c)
        {
            switch (_state)
            {
                case State.Ground:
                    return ProcessGround(c);

                case State.Escape:
                    return ProcessEscape(c);

                case State.CsiEntry:
                case State.CsiParam:
                    return ProcessCsi(c);

                default:
                    Reset();
                    return new PrintResult(c);
            }
        }

        /// <summary>
        /// Resets the parser to ground state.
        /// </summary>
        public void Reset()
        {
            _state = State.Ground;
            _parameters.Clear();
            _currentParam = 0;
            _hasCurrentParam = false;
            _isPrivate = false;
        }

        private ParserResult ProcessGround(char c)
        {
            // Check for control characters
            switch (c)
            {
                case '\x1b': // ESC
                    _state = State.Escape;
                    return NoActionResult.Instance;

                case '\r': // CR
                    return new ControlResult(ControlCode.CarriageReturn);

                case '\n': // LF
                    return new ControlResult(ControlCode.LineFeed);

                case '\t': // TAB
                    return new ControlResult(ControlCode.Tab);

                case '\b': // BS
                    return new ControlResult(ControlCode.Backspace);

                case '\x07': // BEL
                    return new ControlResult(ControlCode.Bell);

                default:
                    // Printable character
                    if (c >= 0x20 && c < 0x7f || c >= 0x80)
                    {
                        return new PrintResult(c);
                    }
                    // Other control characters - ignore
                    return NoActionResult.Instance;
            }
        }

        private ParserResult ProcessEscape(char c)
        {
            switch (c)
            {
                case '[': // CSI introducer
                    _state = State.CsiEntry;
                    _parameters.Clear();
                    _currentParam = 0;
                    _hasCurrentParam = false;
                    _isPrivate = false;
                    return NoActionResult.Instance;

                default:
                    // Unknown escape sequence - abort and print
                    Reset();
                    // For now, just ignore the escape and return to ground
                    return ProcessGround(c);
            }
        }

        private ParserResult ProcessCsi(char c)
        {
            // Check for parameter characters (digits and semicolons)
            if (c >= '0' && c <= '9')
            {
                _state = State.CsiParam;
                _currentParam = _currentParam * 10 + (c - '0');
                _hasCurrentParam = true;
                return NoActionResult.Instance;
            }

            if (c == ';')
            {
                // Parameter separator
                _parameters.Add(_hasCurrentParam ? _currentParam : 0);
                _currentParam = 0;
                _hasCurrentParam = false;
                _state = State.CsiParam;
                return NoActionResult.Instance;
            }

            if (c == '?')
            {
                // Private sequence marker
                _isPrivate = true;
                return NoActionResult.Instance;
            }

            // Check for final byte (0x40-0x7E)
            if (c >= 0x40 && c <= 0x7E)
            {
                // Complete the sequence
                if (_hasCurrentParam)
                {
                    _parameters.Add(_currentParam);
                }

                var sequence = new CsiSequence(_parameters.ToArray(), c, _isPrivate);
                Reset();
                return new SequenceResult(sequence);
            }

            // Intermediate bytes (0x20-0x2F) - collect but we don't use them yet
            if (c >= 0x20 && c <= 0x2F)
            {
                return NoActionResult.Instance;
            }

            // Invalid character in sequence - abort
            Reset();
            return ProcessGround(c);
        }
    }
}
