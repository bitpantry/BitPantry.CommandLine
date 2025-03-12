using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class LocalDiskFileStorageServiceTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            Directory.Delete("./local-file-storage", true);
        }

        [TestMethod]
        public async Task StreamFileToStorage_Success()
        {
            var svc = new LocalDiskFileStorageService();

            var filePath = Path.GetTempFileName();
            var buffer = new byte[81920];
            new Random().NextBytes(buffer);

            using (var stream = new FileStream(filePath, FileMode.Open))
                stream.Write(buffer, 0, buffer.Length);

            var relativeFilePath = "test.txt";
            await svc.AppendBuffer(relativeFilePath, buffer, buffer.Length);
        }

        [TestMethod]
        public async Task StreamFileToStorage_NestedDirectory_Success()
        {
            var svc = new LocalDiskFileStorageService();

            var filePath = Path.GetTempFileName();
            var buffer = new byte[81920];
            new Random().NextBytes(buffer);

            using (var stream = new FileStream(filePath, FileMode.Open))
                stream.Write(buffer, 0, buffer.Length);

            var relativeFilePath = "nested/test.txt";
            await svc.AppendBuffer(relativeFilePath, buffer, buffer.Length);
        }
    }
}
