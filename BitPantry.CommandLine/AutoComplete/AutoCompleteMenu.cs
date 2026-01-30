using System;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.AutoComplete
{
    /// <summary>
    /// Manages the state of an autocomplete dropdown menu.
    /// Handles selection, scrolling, and filtering of options.
    /// </summary>
    public class AutoCompleteMenu
    {
        private readonly List<AutoCompleteOption> _allOptions;
        private List<AutoCompleteOption> _filteredOptions;
        private int _selectedIndex;
        private int _visibleStartIndex;

        /// <summary>
        /// The maximum number of items visible in the menu at once.
        /// </summary>
        public const int MaxVisibleItems = AutoCompleteConstants.DefaultVisibleMenuItems;

        /// <summary>
        /// Gets the original unfiltered options.
        /// </summary>
        public IReadOnlyList<AutoCompleteOption> Options => _allOptions;

        /// <summary>
        /// Gets the currently filtered options (or all options if no filter applied).
        /// </summary>
        public IReadOnlyList<AutoCompleteOption> FilteredOptions => _filteredOptions;

        /// <summary>
        /// Gets the currently selected index within the filtered options.
        /// </summary>
        public int SelectedIndex => _selectedIndex;

        /// <summary>
        /// Gets the currently selected option.
        /// </summary>
        public AutoCompleteOption SelectedOption => 
            _filteredOptions.Count > 0 ? _filteredOptions[_selectedIndex] : null;

        /// <summary>
        /// Gets the start index of the visible window within filtered options.
        /// </summary>
        public int VisibleStartIndex => _visibleStartIndex;

        /// <summary>
        /// Gets the currently visible options (up to MaxVisibleItems).
        /// </summary>
        public IReadOnlyList<AutoCompleteOption> VisibleOptions
        {
            get
            {
                if (_filteredOptions.Count <= MaxVisibleItems)
                    return _filteredOptions;

                return _filteredOptions
                    .Skip(_visibleStartIndex)
                    .Take(MaxVisibleItems)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets whether there are more options above the visible window.
        /// </summary>
        public bool HasMoreAbove => _visibleStartIndex > 0;

        /// <summary>
        /// Gets whether there are more options below the visible window.
        /// </summary>
        public bool HasMoreBelow => 
            _filteredOptions.Count > MaxVisibleItems && 
            (_visibleStartIndex + MaxVisibleItems) < _filteredOptions.Count;

        /// <summary>
        /// Gets the count of options above the visible window.
        /// </summary>
        public int MoreAboveCount => _visibleStartIndex;

        /// <summary>
        /// Gets the count of options below the visible window.
        /// </summary>
        public int MoreBelowCount => 
            Math.Max(0, _filteredOptions.Count - _visibleStartIndex - MaxVisibleItems);

        /// <summary>
        /// Gets whether the menu is empty (no filtered options available).
        /// </summary>
        public bool IsEmpty => _filteredOptions.Count == 0;

        /// <summary>
        /// Creates a new AutoCompleteMenu with the specified options.
        /// </summary>
        /// <param name="options">The autocomplete options to display.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        /// <exception cref="ArgumentException">Thrown when options is empty.</exception>
        public AutoCompleteMenu(List<AutoCompleteOption> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (options.Count == 0)
                throw new ArgumentException("Menu must have at least one option.", nameof(options));

            _allOptions = new List<AutoCompleteOption>(options);
            _filteredOptions = new List<AutoCompleteOption>(options);
            _selectedIndex = 0;
            _visibleStartIndex = 0;
        }

        /// <summary>
        /// Moves the selection down one item, wrapping to the top if at the bottom.
        /// </summary>
        public void MoveDown()
        {
            if (_filteredOptions.Count == 0)
                return;

            _selectedIndex++;
            if (_selectedIndex >= _filteredOptions.Count)
            {
                // Wrap to top
                _selectedIndex = 0;
                _visibleStartIndex = 0;
            }
            else
            {
                EnsureSelectionVisible();
            }
        }

        /// <summary>
        /// Moves the selection up one item, wrapping to the bottom if at the top.
        /// </summary>
        public void MoveUp()
        {
            if (_filteredOptions.Count == 0)
                return;

            _selectedIndex--;
            if (_selectedIndex < 0)
            {
                // Wrap to bottom
                _selectedIndex = _filteredOptions.Count - 1;
                _visibleStartIndex = Math.Max(0, _filteredOptions.Count - MaxVisibleItems);
            }
            else
            {
                EnsureSelectionVisible();
            }
        }

        /// <summary>
        /// Filters the options by the specified query string.
        /// Matching is case-insensitive and uses StartsWith.
        /// </summary>
        /// <param name="query">The query string to filter by.</param>
        /// <returns>True if any options match; false if filter results in empty list.</returns>
        public bool Filter(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                _filteredOptions = new List<AutoCompleteOption>(_allOptions);
            }
            else
            {
                _filteredOptions = _allOptions
                    .Where(o => o.Value.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Reset selection and scroll position
            _selectedIndex = 0;
            _visibleStartIndex = 0;

            return _filteredOptions.Count > 0;
        }

        /// <summary>
        /// Ensures the selected item is within the visible window.
        /// Adjusts the visible window start index if necessary.
        /// </summary>
        private void EnsureSelectionVisible()
        {
            if (_filteredOptions.Count <= MaxVisibleItems)
            {
                _visibleStartIndex = 0;
                return;
            }

            // If selection is above visible window, scroll up
            if (_selectedIndex < _visibleStartIndex)
            {
                _visibleStartIndex = _selectedIndex;
            }
            // If selection is below visible window, scroll down
            else if (_selectedIndex >= _visibleStartIndex + MaxVisibleItems)
            {
                _visibleStartIndex = _selectedIndex - MaxVisibleItems + 1;
            }
        }
    }
}
