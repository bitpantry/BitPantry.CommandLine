using BitPantry.CommandLine.Processing.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete
{
    public record AutoCompleteResult(
        AutoCompleteOption Option = null,
        string AutoCompletedInputString = null,
        int AutoCompleteStartPosition = 0,
        int AutoCompletedCursorPosition = 0,
        int NumPositionsToClearAfterInput = 0);
}
