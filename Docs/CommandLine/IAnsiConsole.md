# IAnsiConsole
BitPantry.CommandLine uses [Spectre Console](https://spectreconsole.net/) to abstract all terminal I/O. If you aren't familiar with Spectre Console, it's an incredible project that "makes it easier to create beautiful console applications." 

When building commands, always use the console exposed by the [CommandBase](CommandBase.md) to make sure your command is correctly handling I/O.