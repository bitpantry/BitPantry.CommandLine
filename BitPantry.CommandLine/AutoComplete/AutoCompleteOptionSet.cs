using System.Collections.Generic;

namespace BitPantry.CommandLine.AutoComplete
{
    /// <summary>
    /// Manages a set of AutoCompleteOptions, allowing the consumer to move to the next or previous option
    /// </summary>
    public class AutoCompleteOptionSet
    {
        private int _currentIndex = 0;
        private readonly IList<AutoCompleteOption> _options;

        /// <summary>
        /// Gets the options managed by the manager
        /// </summary>
        public IReadOnlyList<AutoCompleteOption> Options => _options.AsReadOnly();

        /// <summary>
        /// Gets the currently selected option.
        /// </summary>
        public AutoCompleteOption CurrentOption => _options.Count == 0 ? null : _options[_currentIndex];

        /// <summary>
        /// Creates a new instance of the AutoCompleteOptionsManager
        /// </summary>
        /// <param name="input">The parsed input these options are created for</param>
        public AutoCompleteOptionSet(IList<AutoCompleteOption> options, int startingIndex = 0) 
        {
            _options = options;
            _currentIndex = startingIndex;
        }

        /// <summary>
        /// Moves to the next auto-complete option.
        /// </summary>
        /// <returns>True if advanced to the next option; otherwise, false.</returns>
        public bool NextOption()
        {
            if (_options.Count < 2)
                return false;

            _currentIndex++;

            if (_currentIndex > _options.Count - 1)
                _currentIndex = 0;

            return true;
        }

        /// <summary>
        /// Moves to the previous auto-complete option.
        /// </summary>
        /// <returns>True if moved back to the previous option; otherwise, false.</returns>
        public bool PreviousOption()
        {
            if (_options.Count < 2)
                return false;

            _currentIndex--;

            if (_currentIndex < 0)
                _currentIndex = _options.Count - 1;

            return true;
        }
    }
}
