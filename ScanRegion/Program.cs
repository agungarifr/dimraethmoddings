using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class ScanRegion
{
    static void Main(string[] args)
    {
        string path = args[0];
        long start = long.Parse(args[1]);   // offset of serialized file (0)
        long end = long.Parse(args[2]);     // 0x014833EC = 21512172
        var binStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var samples = new List<(long off, string t)>();

        binStream.Position = start;
        long len = end - start;
        int chunk = 16 * 1024 * 1024;
        long baseOff = start;
        byte[] prevTail = Array.Empty<byte>();
        const int carry = 32;

        while (binStream.Position < end)
        {
            int n = (int)Math.Min(chunk, end - binStream.Position);
            var seg = new byte[n];
            binStream.Read(seg, 0, n);
            var buf = new byte[prevTail.Length + seg.Length];
            Buffer.BlockCopy(prevTail, 0, buf, 0, prevTail.Length);
            Buffer.BlockCopy(seg, 0, buf, prevTail.Length, seg.Length);
            long off0 = baseOff - prevTail.Length;

            // UTF-8 runs
            for (int i = 0; i < buf.Length - 1; i++)
            {
                byte b = buf[i];
                if (b >= 0x20 && b <= 0x7E)
                {
                    var sb = new StringBuilder();
                    int j = i;
                    while (j < buf.Length && buf[j] >= 0x20 && buf[j] <= 0x7E) { sb.Append((char)buf[j]); j++; }
                    if (sb.Length >= 6 && samples.Count < 300)
                    {
                        string t = sb.ToString();
                        bool hasSpace = t.Contains(" ");
                        bool letters = t.IndexOfAny(new char[] { 'e', 'a', 'i', 'o', 'u', 'n', 't', 'r', 'd', 'l', 's' }) >= 0;
                        if (hasSpace && letters)
                            samples.Add((off0 + i, t));
                    }
                    i = j - 1;
                }
            }

            prevTail = new byte[carry];
            Buffer.BlockCopy(buf, buf.Length - carry, prevTail, 0, carry);
            baseOff += seg.Length;
            Console.WriteLine($"scanned {baseOff:n0} / {end:n0}");
        }

        Console.WriteLine($"\n=== {samples.Count} UTF-8 text samples in serialized-file region ===");
        foreach (var (off, t) in samples)
            Console.WriteLine($"  0x{off:X}: {t}");
    }
}