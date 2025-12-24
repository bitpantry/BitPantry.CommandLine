using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Tests.Commands
{
    /// <summary>
    /// Test group for testing group-based command organization.
    /// </summary>
    [Group(Description = "Test group for unit tests")]
    public class TestGroup { }

    /// <summary>
    /// BitPantry test group for testing group-based command organization.
    /// </summary>
    [Group(Name = "bitpantry", Description = "BitPantry test group")]
    public class BitPantryGroup { }

    /// <summary>
    /// First namespace test group for duplicate name tests.
    /// </summary>
    [Group(Name = "ns1", Description = "First test namespace group")]
    public class Ns1Group { }

    /// <summary>
    /// Second namespace test group for duplicate name tests.
    /// </summary>
    [Group(Name = "ns2", Description = "Second test namespace group")]
    public class Ns2Group { }
}
