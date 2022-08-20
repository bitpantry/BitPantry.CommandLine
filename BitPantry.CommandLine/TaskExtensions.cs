using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine
{
    public static class TaskExtensions
    {
        // derived from https://stackoverflow.com/questions/22109246/get-result-of-taskt-without-knowing-typeof-t
        public static async Task<object> ConvertToGenericTaskOfObject(this Task task)
        {
            await task;
            var voidTaskType = typeof(Task<>).MakeGenericType(Type.GetType("System.Threading.Tasks.VoidTaskResult"));
            if (voidTaskType.IsAssignableFrom(task.GetType()))
                throw new InvalidOperationException("Task does not have a return value (" + task.GetType().ToString() + ")");
            var property = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                throw new InvalidOperationException("Task does not have a return value (" + task.GetType().ToString() + ")");
            return property.GetValue(task);
        }
    }
}
