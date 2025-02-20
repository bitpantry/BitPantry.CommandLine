namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    internal static class MessageArgNames
    {
        public static class Common
        {
            public const string CorrelationId = "cid";
        }

        public static class Request
        {
            public const string RequestType = "rt";
        }

        public static class Response
        {
            public const string IsRemoteError = "iree";
        }

        public static class RunResponse
        {
            public const string ResultCode = "rc";
            public const string IsRunError = "irue";
            public const string Result = "rs";
            public const string ResultDataType = "rsdt";
        }

        public static class IsKeyAvailableResponse
        {
            public const string IsAvailable = "ia";
        }

        public static class ReadKeyRequest
        {
            public static string Intercept = "in";
        }

        public static class ReadKeyResponse
        {
            public const string KeyInfo = "ki";
        }

        public static class RunRequest
        {
            public const string ConsoleSettings = "cs";
            public const string CmdLineInputString = "clis";
            public const string PipelineData = "pd";
            public const string PipelineDataType = "pddt";
        }

        public static class AutoCompleteRequest
        {
            public const string CmdNamespace = "cns";
            public const string CmdName = "cn";
            public const string FunctionName = "fn";
            public const string IsFunctionAsync = "ask";
            public const string AutoCompleteContext = "ctx";
        }

        public static class AutoCompleteResponse
        {
            public const string Results = "acr";
        }

        public static class CreateClientResponse
        {
            public const string CommandInfos = "cmi";
        }
    }
}
