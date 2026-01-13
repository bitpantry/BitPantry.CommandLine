using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Remote.SignalR.Environment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    [TestClass]
    public class IntegrationTests_FileTransferService
    {
        [TestMethod]
        public async Task UploadFile_FileUploaded()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var localFilePath = env.FileSystem.CreateLocalFile("test.txt", "test");

            await env.Cli.Services.GetRequiredService<FileTransferService>()
                .UploadFile(localFilePath, $"{env.FileSystem.ServerTestFolderPrefix}/test.txt");

            var serverFilePath = env.FileSystem.LocalPath("../.." + env.FileSystem.ServerTestDir + "/test.txt");
            // Verify via the actual server path
            var actualServerPath = Path.Combine(env.FileSystem.ServerTestDir, "test.txt");
            File.Exists(actualServerPath).Should().BeTrue();
            File.ReadAllText(actualServerPath).Should().Be("test");
        }

        [TestMethod]
        public async Task UploadFileWithProgress_FileUploadedWithProgress()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var data = new string('a', 524288); // 0.5 MB of 'a'
            var localFilePath = env.FileSystem.CreateLocalFile("test-progress.txt", data);

            var progressLines = new List<string>();

            await env.Cli.Services.GetRequiredService<FileTransferService>()
                .UploadFile(localFilePath, $"{env.FileSystem.ServerTestFolderPrefix}/test-progress.txt",
                    prog =>
                    {
                        progressLines.Add(prog.TotalRead.ToString());
                        return Task.CompletedTask;
                    });

            var actualServerPath = Path.Combine(env.FileSystem.ServerTestDir, "test-progress.txt");
            File.Exists(actualServerPath).Should().BeTrue();
            File.ReadAllText(actualServerPath).Should().Be(data);

            progressLines.Count.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public async Task UploadFile_FileNotFound()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var nonExistentFilePath = env.FileSystem.LocalPath("non-existent-file.txt");

            Func<Task> act = async () => await env.Cli.Services.GetRequiredService<FileTransferService>()
                .UploadFile(nonExistentFilePath, "test.txt");

            await act.Should().ThrowAsync<FileNotFoundException>();
        }

        [TestMethod]
        public async Task UploadFile_UploadCanceled()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var data = new string('a', 524288); // 0.5 MB of 'a'
            var localFilePath = env.FileSystem.CreateLocalFile("test-cancel.txt", data);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await env.Cli.Services.GetRequiredService<FileTransferService>()
                .UploadFile(localFilePath, $"{env.FileSystem.ServerTestFolderPrefix}/test-cancel.txt", null, cts.Token);

            await act.Should().ThrowAsync<TaskCanceledException>();
        }

        [TestMethod]
        public async Task UploadFile_ClientDisconnectsDuringUpload()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var data = new string('a', 524288); // 0.5 MB of 'a'
            var localFilePath = env.FileSystem.CreateLocalFile("test-disconnect.txt", data);

            var fileUploadService = env.Cli.Services.GetRequiredService<FileTransferService>();

            var uploadTask = fileUploadService.UploadFile(
                localFilePath, 
                $"{env.FileSystem.ServerTestFolderPrefix}/test-disconnect.txt", 
                null, 
                CancellationToken.None);

            await env.Cli.Services.GetRequiredService<IServerProxy>().Disconnect();

            Func<Task> act = async () => await uploadTask;

            await act.Should().ThrowAsync<Exception>().WithMessage("File upload failed with error: Client disconnected during file upload");
        }

        [TestMethod]
        public async Task UploadFile_ServerDisconnectsDuringUpload()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var data = new string('a', 524288); // 0.5 MB of 'a'
            var localFilePath = env.FileSystem.CreateLocalFile("test-server-disconnect.txt", data);

            var fileUploadService = env.Cli.Services.GetRequiredService<FileTransferService>();

            var uploadTask = fileUploadService.UploadFile(
                localFilePath, 
                $"{env.FileSystem.ServerTestFolderPrefix}/test-server-disconnect.txt", 
                null, 
                CancellationToken.None);

            await env.Server.Host.StopAsync();

            Func<Task> act = async () => await uploadTask;

            await act.Should().ThrowAsync<Exception>().WithMessage("File upload failed with error: Client disconnected during file upload");
        }
    }
}
