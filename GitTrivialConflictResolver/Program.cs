using System;
using System.IO;
using System.Linq;

namespace GitTrivialConflictResolver
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: GitTrivialConflictResolver.exe folder searchPattern");
                return;
            }

            var folder = args[0];
            var searchPattern = args[1];

            if (!Directory.Exists(folder))
            {
                Console.WriteLine($"Directory {folder} does not exists.");
                return;
            }

            var filePaths = Directory.GetFiles(folder, searchPattern);
            if (!filePaths.Any())
            {
                Console.WriteLine($"Directory {folder} does not contain any {searchPattern} files.");
                return;
            }

            ConflictResolver.ResolveInAllFiles(filePaths);
        }
    }
}