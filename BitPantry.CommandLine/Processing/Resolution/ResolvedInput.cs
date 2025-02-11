using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.Processing.Resolution
{
    public class ResolvedInput
    {
        private List<ResolvedCommand> _commands = new List<ResolvedCommand>();

        /// <summary>
        /// Resolved commands in order of input
        /// </summary>
        public IReadOnlyList<ResolvedCommand> ResolvedCommands { get; }

        /// <summary>
        /// Any command resolution errors
        /// </summary>
        public IReadOnlyCollection<ResolveCommandError> CommandErrors => ResolvedCommands.SelectMany(c => c.Errors).ToList().AsReadOnly();

        /// <summary>
        /// Any pipeline errors
        /// </summary>
        public IReadOnlyList<DataPipelineError> DataPipelineErrors { get; } = new List<DataPipelineError>().AsReadOnly();

        /// <summary>
        /// Whether or not the resolution is valid
        /// </summary>
        public bool IsValid => !CommandErrors.Any() && !DataPipelineErrors.Any();

        public ResolvedInput(List<ResolvedCommand> commands)
        {
            ResolvedCommands = commands.AsReadOnly();

            if (CommandErrors.Any()) return;

            // if not any resolution errors then evaluate pipeline errors - impossible to validate pipeline errors without correctly resolved commands

            var pipelineErrors = new List<DataPipelineError>();

            if (commands.Count > 1)
            {
                for (int i = 1; i < ResolvedCommands.Count; i++)
                {
                    if (commands[i - 1].CommandInfo.ReturnType != typeof(void))
                    {
                        if (!ResolvedCommands[i].CommandInfo.InputType.IsAssignableFrom(ResolvedCommands[i - 1].CommandInfo.ReturnType))
                            pipelineErrors.Add(new DataPipelineError(ResolvedCommands[i - 1], ResolvedCommands[i]));
                    }
                }
            }

            DataPipelineErrors = pipelineErrors.AsReadOnly();
        }
    }
}
