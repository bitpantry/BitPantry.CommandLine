using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using Spectre.Console;

var vc = new VirtualConsole(80, 24);
vc.StrictMode = true;
var adapter = new VirtualConsoleAnsiAdapter(vc);

// Write styled text using Spectre
var cyanStyle = new Style(foreground: Color.Cyan);
adapter.Write(new Text("server", cyanStyle));

// Check result
Console.WriteLine($"Text: {vc.GetRow(0).GetText().TrimEnd()}");
var cell = vc.GetCell(0, 0);
Console.WriteLine($"Char: {cell.Character}");
Console.WriteLine($"FG Color: {cell.Style.ForegroundColor}");
Console.WriteLine($"FG 256: {cell.Style.Foreground256}");
Console.WriteLine($"FG RGB: {cell.Style.ForegroundRgb}");
