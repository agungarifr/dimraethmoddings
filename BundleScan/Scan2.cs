using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class Scan2
{
    static int CountBytes(byte[] data, byte[] pat)
    {
        int cnt = 0;
        int limit = data.Length - pat.Length;
        for (int i = 0; i <= limit; i++)
        {
            bool ok = true;
            for (int j = 0; j < pat.Length; j++)
                if (data[i + j] != pat[j]) { ok = false; break; }
            if (ok) { cnt++; i += pat.Length - 1; }
        }
        return cnt;
    }

    static int IndexOf(byte[] data, byte[] pat, int from)
    {
        for (int i = from; i <= data.Length - pat.Length; i++)
        {
            bool ok = true;
            for (int j = 0; j < pat.Length; j++)
                if (data[i + j] != pat[j]) { ok = false; break; }
            if (ok) return i;
        }
        return -1;
    }

    static string HexWords(byte[] data, int off, int len)
    {
        var sb = new StringBuilder(len * 6);
        for (int i = 0; i < len && off + i < data.Length; i++)
        {
            if (i > 0 && i % 2 == 0) sb.Append(' ');
            sb.Append(data[off + i].ToString("X2"));
        }
        return sb.ToString();
    }

    static void Main(string[] args)
    {
        string path = args.Length > 0 ? args[0]
            : @"D:\SteamLibrary\steamapps\common\Dimraeth\Dimraeth_Data\StreamingAssets\aa\StandaloneWindows64\defaultlocalgroup_assets_all_11aa88868962abe284c06cb1ddb7ee3a.decompressed.bin";
        string[] markers = { "HeroicOption", "ScholarOption", "RelationshipBonus", "PlayerDialogueEntry",
            "EntryGUID", "StoicOption", "RelationshipData", "NPCResponseID", "Heroic", "Joker", "Scholar",
            "Relationship", "Persona", "Archetype", "CAB-", ".resS", "TextAsset", "dialogue", "Dialogue" };
        var pats8 = markers.Select(m => Encoding.UTF8.GetBytes(m)).ToArray();
        var pats16 = markers.Select(m => Encoding.Unicode.GetBytes(m)).ToArray();

        var hits = new Dictionary<string, int>();
        var hitOffsets = new List<(string m, long off)>();
        int chunk = 64 * 1024 * 1024;
        var prev = new byte[0];
        long baseOff = 0;

        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        using (var br = new BinaryReader(fs))
        {
            byte[] seg;
            do
            {
                int n = (int)Math.Min(chunk, fs.Length - fs.Position);
                seg = new byte[n];
                br.Read(seg, 0, n);
                var joined = new byte[prev.Length + seg.Length];
                Buffer.BlockCopy(prev, 0, joined, 0, prev.Length);
                Buffer.BlockCopy(seg, 0, joined, prev.Length, seg.Length);
                long off0 = baseOff - prev.Length;

                for (int mi = 0; mi < markers.Length; mi++)
                {
                    string m = markers[mi];
                    int c8 = CountBytes(joined, pats8[mi]);
                    if (c8 > 0) hits[m] = hits.GetValueOrDefault(m) + c8;
                    int c16 = CountBytes(joined, pats16[mi]);
                    if (c16 > 0) hits[m] = hits.GetValueOrDefault(m) + c16;
                    if (hitOffsets.Count < 200)
                    {
                        int from = 0;
                        while (from < joined.Length)
                        {
                            int idx = IndexOf(joined, pats16[mi], from);
                            if (idx < 0) idx = IndexOf(joined, pats8[mi], from);
                            if (idx < 0) break;
                            hitOffsets.Add((m, off0 + idx));
                            from = idx + 1;
                        }
                    }
                }

                int carry = 64;
                prev = new byte[carry];
                int cp = Math.Max(0, joined.Length - carry);
                Buffer.BlockCopy(joined, cp, prev, 0, joined.Length - cp);
                baseOff += seg.Length;
                Console.WriteLine($"scanned {baseOff:n0} / {fs.Length:n0}  hits so far: {string.Join(" ", hits.OrderByDescending(kv => kv.Value).Take(6).Select(kv => $"{kv.Key}={kv.Value}"))}");
            } while (seg != null && fs.Position < fs.Length);
        }

        Console.WriteLine("\n=== FINAL HIT COUNTS ===");
        foreach (var kv in hits.OrderByDescending(k => k.Value))
            Console.WriteLine($"  {kv.Key} = {kv.Value}");

        Console.WriteLine("\n=== FIRST 40 HIT OFFSETS (UTF-16, with context) ===");
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var br = new BinaryReader(fs))
        {
            foreach (var (m, off) in hitOffsets.Take(40))
            {
                int ctx = 32;
                long s = Math.Max(0, off - ctx);
                br.BaseStream.Position = s;
                var buf = new byte[ctx * 2 + 128];
                int rl = br.Read(buf, 0, buf.Length);
                Console.WriteLine($"\n[{m}] at 0x{off:X}:");
                Console.WriteLine("  words: " + HexWords(buf, 0, Math.Min(64, rl)));
                string txt = "";
                for (int i = 0; i + 1 < rl; i += 2)
                {
                    char c = (char)(buf[i] | (buf[i + 1] << 8));
                    if (char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-' || c == '.' || c == ',')
                        txt += c;
                    else txt += '·';
                }
                Console.WriteLine("  text:  " + txt.Substring(0, Math.Min(64, txt.Length)));
            }
        }
    }
}