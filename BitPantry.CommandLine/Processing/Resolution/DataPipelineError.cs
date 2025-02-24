namespace BitPantry.CommandLine.Processing.Resolution
{
    public class DataPipelineError
    {
        public ResolvedCommand FromCommand { get; }
        public ResolvedCommand ToCommand { get; }

        public DataPipelineError(ResolvedCommand fromCommand, ResolvedCommand toCommand)
        {
            FromCommand = fromCommand;
            ToCommand = toCommand;
        }
    }
}