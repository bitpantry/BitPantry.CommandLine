using System.Reflection;
using System.Runtime.CompilerServices;

namespace BitPantry.CommandLine
{
    public static class MethodInfoExtensions
    {
        public static bool IsAsync(this MethodInfo info)
            => info.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;
    }
}
