using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class ScanText
{
    static void Main(string[] args)
    {
        string path = args.Length > 0 ? args[0]
            : @"D:\SteamLibrary\steamapps\common\Dimraeth\Dimraeth_Data\StreamingAssets\aa\StandaloneWindows64\defaultlocalgroup_assets_all_11aa88868962abe284c06cb1ddb7ee3a.decompressed.bin";
        long fileLen = new FileInfo(path).Length;
        Console.WriteLine($"file len {fileLen:n0}");
        int chunk = 128 * 1024 * 1024;
        long baseOff = 0;
        var lz4FrameCount = 0L;
        var unityfsCount = 0L;
        var cabCount = 0L;
        var samples = new List<(long off, string txt)>();
        byte[] prevTail = Array.Empty<byte>();
        const int carry = 32;

        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var br = new BinaryReader(fs))
        {
            while (fs.Position < fs.Length)
            {
                int n = (int)Math.Min(chunk, fs.Length - fs.Position);
                var seg = br.ReadBytes(n);
                var buf = new byte[prevTail.Length + seg.Length];
                Buffer.BlockCopy(prevTail, 0, buf, 0, prevTail.Length);
                Buffer.BlockCopy(seg, 0, buf, prevTail.Length, seg.Length);

                // count LZ4 frame magic 04 22 4D 18
                for (int i = 0; i <= buf.Length - 4; i++)
                    if (buf[i] == 0x04 && buf[i + 1] == 0x22 && buf[i + 2] == 0x4D && buf[i + 3] == 0x18)
                    { lz4FrameCount++; i += 3; }
                // count "UnityFS" sig
                for (int i = 0; i <= buf.Length - 7; i++)
                    if (buf[i] == 'U' && buf[i+1]=='n' && buf[i+2]=='i' && buf[i+3]=='t' && buf[i+4]=='y' && buf[i+5]=='F' && buf[i+6]=='S')
                    { unityfsCount++; i += 6; }
                // count "CAB-"
                for (int i = 0; i <= buf.Length - 4; i++)
                    if (buf[i] == 'C' && buf[i+1]=='A' && buf[i+2]=='B' && buf[i+3]=='-')
                    { cabCount++; i += 3; }

                // scan for UTF-16 readable runs
                long off0 = baseOff - prevTail.Length;
                for (int i = 0; i < buf.Length - 1; i += 2)
                {
                    char c = (char)(buf[i] | (buf[i + 1] << 8));
                    if (c >= 0x20 && c <= 0x7E)
                    {
                        // gather run
                        var sb = new StringBuilder();
                        int j = i;
                        while (j < buf.Length - 1)
                        {
                            char cc = (char)(buf[j] | (buf[j + 1] << 8));
                            if (cc >= 0x20 && cc <= 0x7E) { sb.Append(cc); j += 2; }
                            else break;
                        }
                        if (sb.Length >= 6)
                        {
                            string t = sb.ToString();
                            if (t.IndexOfAny(new char[] { ' ', 'e', 'a', 'i', 'o', 'u', 'n', 't', 'r' }) >= 0)
                            {
                                if (samples.Count < 60)
                                    samples.Add((off0 + i, t));
                                if (t.Contains("Joker") || t.Contains("Heroic") || t.Contains("Stoic") || t.Contains("Scholar") || t.Contains("Relation"))
                                    samples.Add((off0 + i, "[MARK] " + t));
                            }
                            i = j - 2;
                        }
                    }
                }

                prevTail = new byte[carry];
                Buffer.BlockCopy(buf, buf.Length - carry, prevTail, 0, carry);
                baseOff += seg.Length;
                Console.WriteLine($"scanned {baseOff:n0} / {fileLen:n0}  LZ4frames={lz4FrameCount} UnityFS={unityfsCount} CAB-={cabCount} samples={samples.Count}");
            }
        }

        Console.WriteLine($"\nLZ4 frame magics: {lz4FrameCount}, UnityFS sigs: {unityfsCount}, CAB- names: {cabCount}");
        Console.WriteLine("\n=== UTF-16 READABLE SAMPLES ===");
        foreach (var (off, t) in samples.Take(80))
            Console.WriteLine($"  0x{off:X}: {t}");
    }
}