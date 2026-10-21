using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BriefingRoom4DCS.Data
{
    internal sealed class LocalAircraftFiles
    {
        private static readonly Dictionary<string, string> LiveryAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["A-10CII"] = "A-10C_2"
        };

        private readonly Dictionary<string, List<string>> liveryFolders = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> payloadFiles = new(StringComparer.OrdinalIgnoreCase);

        internal LocalAircraftFiles(string saveGamePath)
        {
            if (string.IsNullOrWhiteSpace(saveGamePath))
                return;

            var liveries = Path.Combine(saveGamePath, "Liveries");
            if (Directory.Exists(liveries))
            {
                foreach (var folder in Directory.EnumerateDirectories(liveries))
                {
                    var name = Path.GetFileName(folder);
                    Add(liveryFolders, name, folder);
                    if (LiveryAliases.TryGetValue(name, out var id))
                        Add(liveryFolders, id, folder);
                }
            }

            var payloads = Path.Combine(saveGamePath, "MissionEditor", "UnitPayloads");
            if (Directory.Exists(payloads))
            {
                foreach (var file in Directory.EnumerateFiles(payloads)
                    .Where(x => Path.GetExtension(x).Equals(".lua", StringComparison.OrdinalIgnoreCase)))
                {
                    // Read metadata without executing user Lua or requiring the payload table to parse.
                    var match = Regex.Match(File.ReadAllText(file),
                        """(?m)^\s*(?:\[\s*["']unitType["']\s*\]|unitType)\s*=\s*["']([^"'\r\n]+)["']""");
                    Add(payloadFiles, Path.GetFileNameWithoutExtension(file), file);
                    if (match.Success)
                        Add(payloadFiles, match.Groups[1].Value, file);
                }
            }
        }

        internal IEnumerable<string> GetLiveryFolders(string id) =>
            liveryFolders.TryGetValue(id, out var folders) ? folders : Array.Empty<string>();

        internal IEnumerable<string> GetPayloadFiles(string id) =>
            payloadFiles.TryGetValue(id, out var files) ? files : Array.Empty<string>();

        private static void Add(Dictionary<string, List<string>> map, string id, string path)
        {
            if (!map.TryGetValue(id, out var paths))
                map[id] = paths = new List<string>();
            if (!paths.Contains(path, StringComparer.OrdinalIgnoreCase))
                paths.Add(path);
        }
    }
}
