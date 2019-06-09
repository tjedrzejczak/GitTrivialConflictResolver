using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitTrivialConflictResolver
{
    internal class ConflictResolver
    {
        private readonly string _filePath;

        public ConflictResolver(string filePath)
        {
            _filePath = filePath;
        }

        internal static void ResolveInAllFiles(string[] filePaths)
        {
            foreach (var path in filePaths)
                (new ConflictResolver(path)).Resolve();
        }

        private void Resolve()
        {
            Console.WriteLine();
            Console.WriteLine($"Processing {_filePath} ...");

            var allLines = File.ReadAllLines(_filePath);

            var conflicts = GetAllConflicts(allLines).ToList();

            if (!conflicts.Any())
            {
                Console.WriteLine("=> No conflicts found.");
                return;
            }

            var resolveInfos = conflicts.Select(cc => TryResolve(cc, allLines)).ToList();

            if (!resolveInfos.Any(x => x.CanBeResolved))
            {
                Console.WriteLine("=> No solvable conflicts found.");
                return;
            }

            var resolvableConflicts = resolveInfos.Where(x => x.CanBeResolved).ToList();
            var resolvedLines = GetResolvedLines(allLines, resolvableConflicts);

            File.WriteAllLines(_filePath, resolvedLines, Encoding.UTF8);

            Console.WriteLine($"=> Resolved {resolvableConflicts.Count}/{conflicts.Count} conflicts.");
        }

        private IEnumerable<ConflictInfo> GetAllConflicts(string[] allLines)
        {
            ConflictInfo? ci = null;
            do
            {
                int startPos = (ci.HasValue) ? (ci.Value.EndLine + 1) : 0;
                ci = SearchNextConflict(allLines, startPos);
                if (ci.HasValue)
                    yield return ci.Value;

            } while (ci.HasValue);
        }

        private ConflictInfo? SearchNextConflict(string[] allLines, int startPos)
        {
            var ci = new ConflictInfo();

            int i = startPos;
            for (; i < allLines.Length; i++)
            {
                if (allLines[i].StartsWith("<<<<<<"))
                {
                    ci.StartLine = i;
                    break;
                }
            }

            for (; i < allLines.Length; i++)
            {
                if (allLines[i].StartsWith("======"))
                {
                    ci.MidLine = i;
                    break;
                }
            }

            for (; i < allLines.Length; i++)
            {
                if (allLines[i].StartsWith(">>>>>>"))
                {
                    ci.EndLine = i;
                    return ci;
                }
            }

            return null;
        }

        private ResolveInfo TryResolve(ConflictInfo ci, string[] allLines)
        {
            if (ci.StartLine + 1 == ci.MidLine && ci.MidLine + 1 == ci.EndLine)
                return new ResolveInfo { ConflictInfo = ci, CanBeResolved = true, ResolvedLines = new string[0] };

            string[] linesA = GetLinesBeetween(allLines, ci.StartLine, ci.MidLine);
            string[] linesB = GetLinesBeetween(allLines, ci.MidLine, ci.EndLine);

            if (AllLinesCanBeIgnored(linesA) && AllLinesCanBeIgnored(linesB))
                return new ResolveInfo { ConflictInfo = ci, CanBeResolved = true, ResolvedLines = linesA };

            return new ResolveInfo { ConflictInfo = ci, CanBeResolved = false };
        }

        private string[] GetLinesBeetween(string[] allLines, int fromLine, int toLine)
        {
            return allLines
                .Skip(fromLine + 1)
                .Take(toLine - fromLine - 1)
                .ToArray();
        }

        private bool AllLinesCanBeIgnored(string[] lines)
        {
            return lines.All(x => LineCanBeIgnored(x));
        }

        private bool LineCanBeIgnored(string line)
        {
            return String.IsNullOrWhiteSpace(line)
                  || "GO".Equals(line.Trim());      //SQL
        }

        private List<string> GetResolvedLines(string[] allLines, IEnumerable<ResolveInfo> resolveInfos)
        {
            var buf = new List<string>();
            int pos = 0;

            foreach (var ri in resolveInfos.OrderBy(x => x.ConflictInfo.StartLine))
            {
                buf.AddRange(allLines.Skip(pos).Take(ri.ConflictInfo.StartLine - pos));
                buf.AddRange(ri.ResolvedLines);
                pos = ri.ConflictInfo.EndLine + 1;
            }

            buf.AddRange(allLines.Skip(pos));

            return buf;
        }
    }
}