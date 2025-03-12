using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine
{
    public interface IFileService
    {
        void AppendAllText(string path, string contents);
        void Copy(string sourceFileName, string destFileName);
        void Delete(string path);
        bool Exists(string path);
        byte[] ReadAllBytes(string path);
        string[] ReadAllLines(string path);
        string ReadAllText(string path);
        void WriteAllBytes(string path, byte[] bytes);
        void WriteAllLines(string path, string[] contents);
        void WriteAllText(string path, string contents);
        Stream OpenRead(string path);
        Stream OpenWrite(string path);
        Stream OpenAppend(string path);
    }
}
