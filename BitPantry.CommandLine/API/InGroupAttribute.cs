using System;

namespace BitPantry.CommandLine.API
{
    /// <summary>
    /// Specifies that a command belongs to a group. Use this attribute on command classes
    /// to assign them to a group defined by a marker class decorated with [Group].
    /// </summary>
    /// <typeparam name="T">The group marker type (must have [Group] attribute)</typeparam>
    /// <example>
    /// <code>
    /// [Group]
    /// public class MathGroup { }
    /// 
    /// [InGroup&lt;MathGroup&gt;]
    /// [Description("Adds two numbers")]
    /// public class AddCommand : CommandBase
    /// {
    ///     public void Execute() { }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class InGroupAttribute<T> : Attribute where T : class
    {
        /// <summary>
        /// Gets the type of the group marker class.
        /// </summary>
        public Type GroupType => typeof(T);
    }
}
