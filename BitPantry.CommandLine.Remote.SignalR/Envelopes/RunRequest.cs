﻿using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class RunRequest : ServerRequest
    {
        [JsonIgnore]
        public ConsoleSettingsModel ConsoleSettings
        {
            get { return DeserializeObject<ConsoleSettingsModel>(MessageArgNames.RunRequest.ConsoleSettings); }
            set { SerializeObject(value, MessageArgNames.RunRequest.ConsoleSettings); }
        }

        [JsonIgnore]
        public string CommandLineInputString
        {
            get { return Data[MessageArgNames.RunRequest.CmdLineInputString]; }
            set { Data[MessageArgNames.RunRequest.CmdLineInputString] = value; }
        }

        [JsonIgnore]
        public object PipelineData
        {
            get { return DeserializeObject(MessageArgNames.RunRequest.PipelineData, MessageArgNames.RunRequest.PipelineDataType); }
            set { SerializeObject(value, MessageArgNames.RunRequest.PipelineData, MessageArgNames.RunRequest.PipelineDataType); }
        }

        [JsonConstructor]
        public RunRequest(Dictionary<string, string> data) : base(data)
        {
            RequestType = ServerRequestType.Run;
        }

        public RunRequest(ConsoleSettingsModel consoleSettings, string commandLineInputString, object pipelineData) : base(ServerRequestType.Run)
        {
            ConsoleSettings = consoleSettings;
            CommandLineInputString = commandLineInputString;
            PipelineData = pipelineData;
        }
    }
}
