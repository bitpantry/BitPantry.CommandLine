using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR
{
    public static class ServiceEndpointNames
    {
        public static readonly string FileUpload = "fileupload";
        public static readonly string FileDownload = "filedownload";
        public static readonly string FilesExist = "files/exists";
    }
}
