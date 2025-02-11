using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Client
{
    public class ServerException : Exception
    {
        public string CorrelationId { get; private set; }

        public ServerException(string correlationId, string message)
            : base(message) { CorrelationId = correlationId; }
    }
}
