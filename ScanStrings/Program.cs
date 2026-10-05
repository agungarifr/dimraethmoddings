using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class ScanStrings
{
    static void Main(string[] args)
    {
        // scan for UTF-16LE strings in a byte region, one string per line, with dedup counts
        string path = args[0];
        long start = args.Length > 1 ? long.Parse(args[1]) : 0;
        long end = args.Length > 2 ? long.Parse(args[2]) : new FileInfo(path).Length;
        int minLen = args.Length > 3 ? int.Parse(args[3]) : 4;

        var counts = new Dictionary<string, int>();
        var firstOff = new Dictionary<string, long>();
        var multiLines = new SortedDictionary<long, string>();
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            // buffer whole region in chunks
            long pos = start;
            long chunk = 8 * 1024 * 1024;
            byte[] tail = Array.Empty<byte>();
            while (pos < end)
            {
                int n = (int)Math.Min(chunk, end - pos);
                var seg = new byte[n];
                fs.Position = pos;
                fs.Read(seg, 0, n);
                var buf = new byte[tail.Length + seg.Length];
                Buffer.BlockCopy(tail, 0, buf, 0, tail.Length);
                Buffer.BlockCopy(seg, 0, buf, tail.Length, seg.Length);
                long absBase = pos - tail.Length;

                for (int i = 0; i < buf.Length; i++)
                {
                    // look for UTF-16 sequences
                    int j = i;
                    var sb = new StringBuilder();
                    while (j + 1 < buf.Length)
                    {
                        char c = (char)(buf[j] | (buf[j + 1] << 8));
                        if (c >= 0x20 && c < 0x7F) { sb.Append(c); j += 2; }
                        else break;
                    }
                    if (sb.Length >= minLen)
                    {
                        string s = sb.ToString();
                        counts.TryGetValue(s, out var c);
                        counts[s] = c + 1;
                        if (!firstOff.ContainsKey(s)) firstOff[s] = absBase + i;
                        if (counts[s] <= 3) multiLines[absBase + i] = s;
                        i = j - 1;
                    }
                }
                // keep tail for cross-chunk strings
                tail = new byte[2 * minLen];
                Buffer.BlockCopy(buf, buf.Length - tail.Length, tail, 0, tail.Length);
                pos += seg.Length;
            }
        }

        Console.WriteLine($"Region {start:X}..{end:X} ({end - start} bytes), minLen={minLen}");
        Console.WriteLine($"total unique strings: {counts.Count}");
        var interesting = counts
            .OrderByDescending(kv => kv.Value)
            .Where(kv => kv.Key.Length >= 5 && kv.Key.Any(ch => char.IsLetter(ch)))
            .Take(80);
        Console.WriteLine("\n=== TOP STRINGS BY FREQUENCY ===");
        foreach (var kv in interesting)
            Console.WriteLine($"  {kv.Value,4}x 0x{firstOff[kv.Key]:X8}  {Sanitize(kv.Key)}");
    }

    static string Sanitize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char c in s) sb.Append(char.IsControl(c) ? '?' : c);
        return sb.ToString();
    }
}