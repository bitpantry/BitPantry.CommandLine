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
            public const string GroupPath = "gp";
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
            public const string ConnectionId = "cnid";
            public const string CommandInfos = "cmi";
            public const string MaxFileSizeBytes = "mfsb";
        }

        public static class  PushMessage
        {
            public const string MessageType = "mt";
        }

        public static class FileUploadProgressUpdate
        {
            public const string TotalBytes = "tb";
            public const string TotalRead = "tr";
            public const string Error = "err";    
        }

        public static class FileDownloadProgressUpdate
        {
            public const string TotalRead = "dtr";
            public const string TotalSize = "dts";
            public const string Error = "derr";
        }

        public static class FileSystem
        {
            public const string Path = "pth";
            public const string SearchPattern = "sp";
            public const string SearchOption = "so";
            public const string Recursive = "rec";
            public const string Exists = "ex";
            public const string Files = "fls";
            public const string Directories = "drs";
            public const string FileInfo = "fi";
            public const string Length = "len";
            public const string CreationTime = "ct";
            public const string LastWriteTime = "lwt";
            public const string LastAccessTime = "lat";
            public const string Attributes = "attr";
            public const string IsReadOnly = "ro";
            public const string Error = "err";
            public const string ErrorCode = "ec";
        }
    }
}
