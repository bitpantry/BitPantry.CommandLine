using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine
{
    public static class MethodInfoExtensions
    {
        public static bool IsAsync(this MethodInfo info)
            => info.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;
    }
}
