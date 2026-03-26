namespace BitPantry.CommandLine.Tests.PluginModule
{
    /// <summary>
    /// Interface for a test service registered by the plugin module.
    /// </summary>
    public interface IPluginService
    {
        string GetValue();
    }

    /// <summary>
    /// Implementation of the test service.
    /// </summary>
    public class PluginService : IPluginService
    {
        private readonly string _value;

        public PluginService(string value)
        {
            _value = value;
        }

        public string GetValue() => _value;
    }
}
