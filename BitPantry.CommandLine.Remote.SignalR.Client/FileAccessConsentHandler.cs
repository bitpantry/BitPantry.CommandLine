using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Spectre.Console;
using System.IO.Abstractions;
using GlobbingDirectoryInfoWrapper = Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Manages the client-side consent UX for server-initiated file access requests.
    /// Checks FileAccessConsentPolicy first, then prompts the user if needed.
    /// Serializes concurrent consent requests so only one prompt displays at a time.
    /// </summary>
    public class FileAccessConsentHandler
    {
        private const int SmallBatchThreshold = 10;
        private const int MediumBatchThreshold = 50;

        private readonly FileAccessConsentPolicy _policy;
        private readonly IAnsiConsole _console;
        private readonly IFileSystem _fileSystem;
        private readonly SemaphoreSlim _promptLock = new(1, 1);

        public FileAccessConsentHandler(
            FileAccessConsentPolicy policy,
            IAnsiConsole console,
            IFileSystem fileSystem)
        {
            _policy = policy;
            _console = console;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Requests consent for a single file path. If the path is already allowed by policy,
        /// returns true immediately without prompting. Otherwise, pauses console output,
        /// renders a consent prompt, reads the user's Y/N response, then resumes output.
        /// </summary>
        /// <param name="path">The file path being requested.</param>
        /// <param name="pauseOutput">Action to pause console output buffering.</param>
        /// <param name="resumeOutput">Action to resume console output and flush buffered output.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if consent is granted, false otherwise.</returns>
        public async Task<bool> RequestConsentAsync(string path, Action pauseOutput, Action resumeOutput, CancellationToken ct)
        {
            if (_policy.IsAllowed(path))
                return true;

            await _promptLock.WaitAsync(ct);
            var outputPaused = false;
            try
            {
                pauseOutput();
                outputPaused = true;
                await Task.Delay(50, ct); // let in-flight output arrive

                var panel = new Panel($"Server requests: [bold]{Markup.Escape(path)}[/]\nAllow? [green]y[/]/[red]N[/]")
                {
                    Header = new PanelHeader("File Access Request"),
                    BorderStyle = new Style(Color.Yellow)
                };
                _console.Write(panel);

                var key = _console.Input.ReadKey(intercept: true);
                var allowed = key?.Key == ConsoleKey.Y;

                return allowed;
            }
            finally
            {
                if (outputPaused)
                    resumeOutput();
                _promptLock.Release();
            }
        }

        /// <summary>
        /// Requests batch consent for a glob enumeration. Evaluates each matched file path
        /// against the policy and prompts for any paths requiring consent.
        /// Uses a tiered display based on file count.
        /// </summary>
        /// <param name="paths">The matched file paths.</param>
        /// <param name="sizes">The file sizes corresponding to each path.</param>
        /// <param name="globPattern">The original glob pattern for display.</param>
        /// <param name="pauseOutput">Action to pause console output buffering.</param>
        /// <param name="resumeOutput">Action to resume console output and flush buffered output.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if consent is granted for all files, false otherwise.</returns>
        public async Task<bool> RequestBatchConsentAsync(
            IReadOnlyList<string> paths,
            IReadOnlyList<long> sizes,
            string globPattern,
            Action pauseOutput,
            Action resumeOutput,
            CancellationToken ct)
        {
            // Check if all paths are pre-allowed
            var pathsRequiringConsent = _policy.GetPathsRequiringConsent(paths);
            if (pathsRequiringConsent.Count == 0)
                return true;

            await _promptLock.WaitAsync(ct);
            var outputPaused = false;
            try
            {
                pauseOutput();
                outputPaused = true;
                await Task.Delay(50, ct);

                var content = BuildBatchConsentContent(paths, sizes, globPattern);
                var panel = new Panel(content)
                {
                    Header = new PanelHeader("File Access Request"),
                    BorderStyle = new Style(Color.Yellow)
                };
                _console.Write(panel);

                var key = _console.Input.ReadKey(intercept: true);
                var allowed = key?.Key == ConsoleKey.Y;

                return allowed;
            }
            finally
            {
                if (outputPaused)
                    resumeOutput();
                _promptLock.Release();
            }
        }

        /// <summary>
        /// Expands a glob pattern on the local file system and returns matching file info entries.
        /// </summary>
        internal List<(string Path, long Size, DateTime LastWriteTimeUtc)> ExpandGlobLocally(string globPattern)
        {
            var (baseDir, pattern) = GlobPatternHelper.ParseGlobPattern(globPattern, _fileSystem);

            if (!_fileSystem.Directory.Exists(baseDir))
                return new List<(string, long, DateTime)>();

            var originalPattern = pattern;
            var matcherPattern = pattern.Replace('?', '*');

            var matcher = new Matcher();
            matcher.AddInclude(matcherPattern);

            var directoryInfo = new DirectoryInfo(baseDir);
            var result = matcher.Execute(new GlobbingDirectoryInfoWrapper(directoryInfo));

            var matchedFiles = result.Files
                .Select(f => _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(baseDir, f.Path)))
                .ToList();

            // Apply ? wildcard filtering
            var filteredFiles = GlobPatternHelper.ApplyQuestionMarkFilter(
                matchedFiles,
                originalPattern,
                _fileSystem.Path.GetFileName);

            return filteredFiles
                .Select(f =>
                {
                    var info = _fileSystem.FileInfo.New(f);
                    return (Path: f, Size: info.Length, LastWriteTimeUtc: info.LastWriteTimeUtc);
                })
                .ToList();
        }

        private static string BuildBatchConsentContent(
            IReadOnlyList<string> paths,
            IReadOnlyList<long> sizes,
            string globPattern)
        {
            var totalSize = sizes.Sum();
            var count = paths.Count;

            if (count <= SmallBatchThreshold)
            {
                // Small batch: show all files
                var lines = new List<string>
                {
                    $"Server requests {count} files matching [bold]{Markup.Escape(globPattern)}[/]:"
                };
                for (int i = 0; i < count; i++)
                {
                    lines.Add($"  {Markup.Escape(paths[i])} ({FormatSize(sizes[i])})");
                }
                lines.Add($"Allow all? [green]y[/]/[red]N[/]");
                return string.Join("\n", lines);
            }

            if (count <= MediumBatchThreshold)
            {
                // Medium batch: first 5, last 2, collapsed middle
                var lines = new List<string>
                {
                    $"Server requests {count} files matching [bold]{Markup.Escape(globPattern)}[/]:"
                };
                for (int i = 0; i < Math.Min(5, count); i++)
                {
                    lines.Add($"  {Markup.Escape(paths[i])} ({FormatSize(sizes[i])})");
                }
                var remainingCount = count - 7; // 5 head + 2 tail
                if (remainingCount > 0)
                {
                    lines.Add($"  ... and {remainingCount} more (total: {FormatSize(totalSize)})");
                }
                for (int i = Math.Max(5, count - 2); i < count; i++)
                {
                    lines.Add($"  {Markup.Escape(paths[i])} ({FormatSize(sizes[i])})");
                }
                lines.Add($"Allow all {count} files? [green]y[/]/[red]N[/]");
                return string.Join("\n", lines);
            }

            // Large batch: summary only
            return string.Join("\n", new[]
            {
                $"Server requests {count} files matching [bold]{Markup.Escape(globPattern)}[/]",
                $"Total size: {FormatSize(totalSize)}",
                $"Allow all {count} files? [green]y[/]/[red]N[/]"
            });
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        }
    }
}
