using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.Input
{
    public class InputLog
    {
        public List<string> _log = new List<string>();
        public int _currentIndex = 0;

        public bool Previous()
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                return true;
            }

            return false;
        }

        public bool Next()
        {
            if(_currentIndex < _log.Count - 1)
            {
                _currentIndex++;
                return true;
            }

            return false;
        }

        public void Add(string entry)
        {
            if(_log.Contains(entry))
                _log.Remove(entry);

            _log.Add(entry);
            _currentIndex = _log.Count;
        }

        public void WriteLineAtCurrentIndex(ConsoleLineMirror inputLine)
        {
            var logLine = _log.Count > 0 ? _log[_currentIndex] : string.Empty;

            inputLine.MoveToPosition(0);
            inputLine.Write(logLine);
            inputLine.Clear(inputLine.BufferPosition);
            inputLine.MoveToPosition(logLine.Length);
        }

        /// <summary>
        /// Gets all history entries, most recent first.
        /// </summary>
        /// <returns>History entries in reverse chronological order.</returns>
        public IEnumerable<string> GetAll()
        {
            // Return in reverse order (most recent first)
            return _log.AsEnumerable().Reverse();
        }

        /// <summary>
        /// Gets the number of entries in the history.
        /// </summary>
        public int Count => _log.Count;
    }
}
