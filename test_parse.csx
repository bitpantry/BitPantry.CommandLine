using BitPantry.CommandLine.Processing.Parsing;
using System;
using System.Linq;

var input = new ParsedCommand("positionalWithNamedCommand --force -- -file.txt dest.txt");
Console.WriteLine("Elements:");
foreach (var elem in input.Elements) {
    if (elem.ElementType == CommandElementType.Empty) continue;
    Console.WriteLine($"  [{elem.Index}] {elem.ElementType,-18} Raw='{elem.Raw,-20}' Value='{elem.Value,-15}' IsAfterEndOfOptions={elem.IsAfterEndOfOptions}");
}
