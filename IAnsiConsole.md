# IAnsiConsole

[Spectr.Console](https://spectreconsole.net/) provides numerous services and abstractions to the System.Console that "make it easier to create beautiful console applications." If you haven't heard of Spectr.Console before, it's an awesome project that made it easy to take this project to the next level.

The central interface provided by Spectr.Console is the ```IAnsiConsole``` interface. You can configure a custom implementation of the interface using the ```CommandLineApplicationBuilder```. If one is not configured the default implementation is used - ```AnsiApplication.Create```.

The implementation is made available to commands via the ```CommandBase``` class.

---
See also,

- [CommandBase](CommandBase.md)
- [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md)