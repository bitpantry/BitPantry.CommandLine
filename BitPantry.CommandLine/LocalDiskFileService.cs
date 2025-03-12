using BitPantry.CommandLine;
using System.IO;
using System;

public class LocalDiskFileService : IFileService
{
    private string _rootDirectoryPath;

    public LocalDiskFileService(string rootDirectoryPath = null)
    {
        _rootDirectoryPath = rootDirectoryPath;
    }

    private string GetFullPath(string path)
    {
        if (_rootDirectoryPath != null)
        {
            if (Path.IsPathRooted(path))
                throw new InvalidOperationException("path must be relative");

            return Path.Combine(_rootDirectoryPath, path);
        }

        return path;
    }

    private void EnsureDirectoryExists(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    public void AppendAllText(string path, string contents)
    {
        var fullPath = GetFullPath(path);
        EnsureDirectoryExists(fullPath);
        File.AppendAllText(fullPath, contents);
    }

    public void Copy(string sourceFileName, string destFileName)
    {
        var fullDestPath = GetFullPath(destFileName);
        EnsureDirectoryExists(fullDestPath);
        File.Copy(GetFullPath(sourceFileName), fullDestPath);
    }

    public void Delete(string path)
        => File.Delete(GetFullPath(path));

    public bool Exists(string path)
        => File.Exists(GetFullPath(path));

    public byte[] ReadAllBytes(string path)
        => File.ReadAllBytes(GetFullPath(path));

    public string[] ReadAllLines(string path)
        => File.ReadAllLines(GetFullPath(path));

    public string ReadAllText(string path)
        => File.ReadAllText(GetFullPath(path));

    public void WriteAllBytes(string path, byte[] bytes)
    {
        var fullPath = GetFullPath(path);
        EnsureDirectoryExists(fullPath);
        File.WriteAllBytes(fullPath, bytes);
    }

    public void WriteAllLines(string path, string[] contents)
    {
        var fullPath = GetFullPath(path);
        EnsureDirectoryExists(fullPath);
        File.WriteAllLines(fullPath, contents);
    }

    public void WriteAllText(string path, string contents)
    {
        var fullPath = GetFullPath(path);
        EnsureDirectoryExists(fullPath);
        File.WriteAllText(fullPath, contents);
    }

    public Stream OpenRead(string path)
        => File.OpenRead(GetFullPath(path));

    public Stream OpenWrite(string path)
    {
        var fullPath = GetFullPath(path);
        EnsureDirectoryExists(fullPath);
        return File.OpenWrite(fullPath);
    }

    public Stream OpenAppend(string path)
    {
        var fullPath = GetFullPath(path);
        EnsureDirectoryExists(fullPath);
        return File.Open(fullPath, FileMode.Append);
    }
}
