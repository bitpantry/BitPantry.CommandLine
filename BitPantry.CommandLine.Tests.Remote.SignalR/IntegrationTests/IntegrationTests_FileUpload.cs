using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Remote.SignalR.Environment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    [TestClass]
    public class IntegrationTests_FileUpload
    {
        [TestMethod]
        public async Task UploadFile_FileUploaded()
        {
            var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "test");

            await env.Cli.Services.GetRequiredService<FileUploadService>().UploadFile(tempFilePath, "test.txt");

            var serverFilePath = Path.Combine("./local-file-storage", "test.txt");

            File.Exists(serverFilePath).Should().BeTrue();
            File.ReadAllText(serverFilePath).Should().Be("test");
        }

        [TestMethod]
        public async Task UploadFileWithProgress_FileUploadedWithProgress()
        {
            var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var tempFilePath = Path.GetTempFileName();
            var data = new string('a', 524288); // 0.5 MB of 'a'
            File.WriteAllText(tempFilePath, data);

            var progressLines = new List<string>();

            await env.Cli.Services.GetRequiredService<FileUploadService>().UploadFile(tempFilePath, "test2.txt",
                prog =>
                {
                    progressLines.Add(prog.TotalRead.ToString());
                    return Task.CompletedTask;
                });

            var serverFilePath = Path.Combine("./local-file-storage", "test2.txt");

            File.Exists(serverFilePath).Should().BeTrue();
            File.ReadAllText(serverFilePath).Should().Be(data);

            progressLines.Count.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public async Task UploadFile_FileNotFound()
        {
            var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var nonExistentFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            Func<Task> act = async () => await env.Cli.Services.GetRequiredService<FileUploadService>().UploadFile(nonExistentFilePath, "test.txt");

            await act.Should().ThrowAsync<FileNotFoundException>();
        }

        [TestMethod]
        public async Task UploadFile_UploadCanceled()
        {
            var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var tempFilePath = Path.GetTempFileName();
            var data = new string('a', 524288); // 0.5 MB of 'a'
            File.WriteAllText(tempFilePath, data);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await env.Cli.Services.GetRequiredService<FileUploadService>().UploadFile(tempFilePath, "test3.txt", null, cts.Token);

            await act.Should().ThrowAsync<TaskCanceledException>();
        }

        [TestMethod]
        public async Task UploadFile_ClientDisconnectsDuringUpload()
        {
            var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var tempFilePath = Path.GetTempFileName();
            var data = new string('a', 524288); // 0.5 MB of 'a'
            File.WriteAllText(tempFilePath, data);

            var fileUploadService = env.Cli.Services.GetRequiredService<FileUploadService>();

            var uploadTask = fileUploadService.UploadFile(tempFilePath, "test5.txt", null, CancellationToken.None);

            await env.Cli.Services.GetRequiredService<IServerProxy>().Disconnect();

            Func<Task> act = async () => await uploadTask;

            await act.Should().ThrowAsync<Exception>().WithMessage("File upload failed with error: Client disconnected during file upload");
        }

        [TestMethod]
        public async Task UploadFile_ServerDisconnectsDuringUpload()
        {
            var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var tempFilePath = Path.GetTempFileName();
            var data = new string('a', 524288); // 0.5 MB of 'a'
            File.WriteAllText(tempFilePath, data);

            var fileUploadService = env.Cli.Services.GetRequiredService<FileUploadService>();

            var uploadTask = fileUploadService.UploadFile(tempFilePath, "test6.txt", null, CancellationToken.None);

            await env.Server.Host.StopAsync();

            Func<Task> act = async () => await uploadTask;

            await act.Should().ThrowAsync<Exception>().WithMessage("File upload failed with error: Client disconnected during file upload");
        }
    }
}
