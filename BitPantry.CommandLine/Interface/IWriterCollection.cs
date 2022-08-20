using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Interface
{
    public interface IWriterCollection
    {
        Writer Info { get; }
        Writer Warning { get; }
        Writer Error { get; }
        Writer Debug { get; }
        Writer Verbose { get; }
    }
}
