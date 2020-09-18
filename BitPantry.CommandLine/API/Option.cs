namespace BitPantry.CommandLine.API
{
    public class Option
    {
        public bool IsPresent { get; }

        internal Option(bool isPresent)
        {
            IsPresent = isPresent;
        }
    }
}
