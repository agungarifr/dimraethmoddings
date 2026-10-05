using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class ScanMarkers
{
    static readonly string[] Markers =
    {
        "HeroicOption", "StoicOption", "JokerOption", "ScholarOption",
        "RelationshipBonus", "PlayerDialogueEntry", "EntryGUID", "NPCResponseID",
        "DialogueID", "DialogueText", "NPCLine", "PlayerLine", "ResponseID",
        "RelationshipData", "ConversationArchetype", "NPC_ID", "NPCName",
    };

    static void Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0]
            : @"D:\SteamLibrary\steamapps\common\Dimraeth\Dimraeth_Data";

        var files = new List<string>();
        // recurse over whole dir, all files (including decompressed bins)
        files.AddRange(Directory.GetFiles(dir, "*", SearchOption.AllDirectories));
        files = files.OrderBy(f => f).ToList();

        foreach (var path in files)
        {
            long len = new FileInfo(path).Length;
            Console.WriteLine($"\n=== {Path.GetFileName(path)} ({len:n0}) ===");
            ScanFile(path);
        }
    }

    static void ScanFile(string path)
    {
        long len = new FileInfo(path).Length;
        int chunk = 128 * 1024 * 1024;
        long baseOff = 0;
        byte[] prev = Array.Empty<byte>();
        var found = new Dictionary<string, List<long>>();
        var contexts = new Dictionary<string, string>();

        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            while (baseOff < len)
            {
                int n = (int)Math.Min(chunk, len - baseOff);
                var seg = new byte[n];
                fs.Position = baseOff;
                fs.Read(seg, 0, n);
                var buf = new byte[prev.Length + seg.Length];
                Buffer.BlockCopy(prev, 0, buf, 0, prev.Length);
                Buffer.BlockCopy(seg, 0, buf, prev.Length, seg.Length);
                long off0 = baseOff - prev.Length;

                foreach (var m in Markers)
                {
                    // UTF-8
                    int p = 0;
                    var nb = Encoding.UTF8.GetBytes(m);
                    while ((p = IndexOf(buf, nb, p)) >= 0)
                    {
                        if (!found.TryGetValue(m, out var l)) { l = new List<long>(); found[m] = l; }
                        if (l.Count < 3) l.Add(off0 + p);
                        if (!contexts.ContainsKey(m))
                            contexts[m] = ReadCtx(buf, p);
                        p += nb.Length;
                    }
                    // UTF-16LE
                    var nb16 = Encoding.Unicode.GetBytes(m);
                    int q = 0;
                    while ((q = IndexOf(buf, nb16, q)) >= 0)
                    {
                        string key = m + "(u16)";
                        if (!found.TryGetValue(key, out var l)) { l = new List<long>(); found[key] = l; }
                        if (l.Count < 3) l.Add(off0 + q);
                        if (!contexts.ContainsKey(key))
                            contexts[key] = ReadCtx(buf, q);
                        q += nb16.Length;
                    }
                }

                // carry tail
                int carry = 64;
                prev = new byte[Math.Min(carry, buf.Length)];
                Buffer.BlockCopy(buf, buf.Length - prev.Length, prev, 0, prev.Length);
                baseOff += seg.Length;
            }
        }

        if (found.Count == 0)
        {
            Console.WriteLine("  (no marker hits)");
            return;
        }
        foreach (var kv in found.OrderBy(k => k.Key))
        {
            Console.WriteLine($"  {kv.Key}: {string.Join(", ", kv.Value.Select(o => $"0x{o:X}"))}");
            if (contexts.TryGetValue(kv.Key, out var ctx))
                Console.WriteLine($"      ctx: {Sanitize(ctx)}");
        }
    }

    static int IndexOf(byte[] hay, byte[] needle, int start)
    {
        int limit = hay.Length - needle.Length;
        for (int i = start; i <= limit; i++)
        {
            bool ok = true;
            for (int j = 0; j < needle.Length; j++)
                if (hay[i + j] != needle[j]) { ok = false; break; }
            if (ok) return i;
        }
        return -1;
    }

    static string ReadCtx(byte[] buf, int p)
    {
        int s = Math.Max(0, p - 120);
        int l = Math.Min(400, buf.Length - s);
        var seg = new byte[l];
        Buffer.BlockCopy(buf, s, seg, 0, l);
        // prefer utf-8 decode
        var s8 = Encoding.UTF8.GetString(seg);
        return s8;
    }

    static string Sanitize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char c in s) sb.Append(char.IsControl(c) ? ' ' : c);
        return sb.ToString();
    }
}
