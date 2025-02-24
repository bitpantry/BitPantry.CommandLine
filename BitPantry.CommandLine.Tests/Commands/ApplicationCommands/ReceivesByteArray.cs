using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    public class ReceivesByteArray : CommandBase
    {
        public void Execute(CommandExecutionContext<byte[]> ctx)
        {
            if (ctx.Input == null)
                throw new ArgumentException("Received a null input");
        }
    }
}
