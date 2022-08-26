//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace BitPantry.CommandLine
//{
//    /// <summary>
//    /// The results of a command execution
//    /// </summary>
//    public class CommandExecutionOutput
//    {
//        /// <summary>
//        /// If true, the Value property contains the data returned from the command execution. If false, the 
//        /// command exeuction function returned void
//        /// </summary>
//        public bool HasValue { get; } = false;

//        /// <summary>
//        /// The value returned by the command execution (check HasValue first)
//        /// </summary>
//        public object Value { get; }

//        /// <summary>
//        /// Instantiates an empty CommandExecutionResult
//        /// </summary>
//        public CommandExecutionOutput() { }

//        /// <summary>
//        /// Instantiates a CommandExecutionResult with a value
//        /// </summary>
//        /// <param name="value">The value</param>
//        public CommandExecutionOutput(object value)
//        {
//            Value = value;
//            HasValue = true;
//        }
//    }
//}
