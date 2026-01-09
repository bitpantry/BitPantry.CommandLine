// Check if FileSystemGlobbing treats ? correctly
var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString());
System.IO.Directory.CreateDirectory(tempDir);
System.IO.File.WriteAllText(System.IO.Path.Combine(tempDir, "data1.json"), "{}");
System.IO.File.WriteAllText(System.IO.Path.Combine(tempDir, "data2.json"), "{}");
System.IO.File.WriteAllText(System.IO.Path.Combine(tempDir, "data10.json"), "{}");

System.Console.WriteLine($"Testing in: {tempDir}");
System.Console.WriteLine($"Files: {string.Join(", ", System.IO.Directory.GetFiles(tempDir).Select(System.IO.Path.GetFileName))}");

var matcher = new Microsoft.Extensions.FileSystemGlobbing.Matcher();
matcher.AddInclude("data?.json");

var di = new System.IO.DirectoryInfo(tempDir);
var wrapper = new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(di);
var result = matcher.Execute(wrapper);

System.Console.WriteLine($"Matcher found: {result.Files.Count()} files");
foreach (var f in result.Files) System.Console.WriteLine($"  - {f.Path}");

System.IO.Directory.Delete(tempDir, true);
