using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
{
    /// <summary>
    /// Parent group for nested group testing.
    /// </summary>
    [Group(Name = "parent")]
    [Description("Parent group for autocomplete tests")]
    public class ParentGroup 
    { 
        /// <summary>
        /// Child group nested under parent group.
        /// </summary>
        [Group(Name = "child")]
        [Description("Child group for autocomplete tests")]
        public class ChildGroup { }
    }

    /// <summary>
    /// Command in parent group for autocomplete testing.
    /// </summary>
    [Command(Group = typeof(ParentGroup), Name = "parentcmd")]
    public class ParentGroupCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Command in child group for autocomplete testing.
    /// </summary>
    [Command(Group = typeof(ParentGroup.ChildGroup), Name = "childcmd")]
    public class ChildGroupCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Another command in child group for multiple command autocomplete testing.
    /// </summary>
    [Command(Group = typeof(ParentGroup.ChildGroup), Name = "anothercmd")]
    public class AnotherChildGroupCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
