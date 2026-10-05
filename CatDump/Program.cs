using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

class CatDump
{
    static void Main(string[] args)
    {
        string path = args.Length > 0 ? args[0]
            : @"D:\SteamLibrary\steamapps\common\Dimraeth\Dimraeth_Data\StreamingAssets\aa\catalog.bin";
        var bytes = File.ReadAllBytes(path);
        // collect all printable strings >= 4 chars (UTF8)
        var set = new HashSet<string>();
        var sb = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            if (b >= 0x20 && b <= 0x7E) { sb.Append((char)b); }
            else
            {
                if (sb.Length >= 4) set.Add(sb.ToString());
                sb.Clear();
            }
        }
        if (sb.Length >= 4) set.Add(sb.ToString());

        Console.WriteLine($"total strings: {set.Count}");
        foreach (var s in set.OrderBy(s => s))
            Console.WriteLine($"  {s}");
        Console.WriteLine("\n=== interesting keys ===");
        var interesting = set.Where(s =>
            s.IndexOf("Dialogue", StringComparison.OrdinalIgnoreCase) >= 0 ||
            s.IndexOf("dialog", StringComparison.OrdinalIgnoreCase) >= 0 ||
            s.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 ||
            s.IndexOf("Greeting", StringComparison.OrdinalIgnoreCase) >= 0 ||
            s.IndexOf("NPC", StringComparison.OrdinalIgnoreCase) >= 0 ||
            s.IndexOf("Conversation", StringComparison.OrdinalIgnoreCase) >= 0 ||
            s.IndexOf("CSV", StringComparison.OrdinalIgnoreCase) >= 0 ||
            s.IndexOf("Topic", StringComparison.OrdinalIgnoreCase) >= 0 ||
            s.IndexOf(".csv", StringComparison.OrdinalIgnoreCase) >= 0 ||
            s.EndsWith(".asset") || s.EndsWith(".controller") || s.EndsWith(".prefab")
        ).OrderBy(s => s).ToList();
        Console.WriteLine("\n=== interesting keys ===");
        foreach (var s in interesting)
            Console.WriteLine($"  {s}");
    }
}
