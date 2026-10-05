using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class DirDump
{
    static int BE32(byte[] b, int p) =>
        (b[p] << 24) | (b[p + 1] << 16) | (b[p + 2] << 8) | b[p + 3];
    static long BE64(byte[] b, int p) =>
        ((long)BE32(b, p) << 32) | (uint)BE32(b, p + 4);

    static byte[] Lz4BlockDecompress(byte[] src, int uncompressedSize)
    {
        byte[] dst = new byte[uncompressedSize];
        int sp = 0, dp = 0;
        while (sp < src.Length && dp < dst.Length)
        {
            int token = src[sp++];
            int litLen = token >> 4;
            if (litLen == 15) { byte b; do { b = src[sp++]; litLen += b; } while (b == 255); }
            if (sp + litLen > src.Length) break;
            Array.Copy(src, sp, dst, dp, litLen);
            sp += litLen; dp += litLen;
            if (dp >= dst.Length) break;
            if (sp + 2 > src.Length) break;
            int offset = src[sp] | (src[sp + 1] << 8);
            sp += 2;
            if (offset == 0) break;
            int matchLen = token & 0xF;
            if (matchLen == 15) { byte b; do { b = src[sp++]; matchLen += b; } while (b == 255); }
            matchLen += 4;
            for (int i = 0; i < matchLen && dp < dst.Length; i++) { dst[dp] = dst[dp - offset]; dp++; }
        }
        return dst;
    }

    static void Main(string[] args)
    {
        string bundlePath = args[0];
        string binPath = Path.ChangeExtension(bundlePath, null) + ".decompressed.bin";
        var f = File.ReadAllBytes(bundlePath);
        var bin = File.ReadAllBytes(binPath);

        // header
        int p = 8; int ver = BE32(f, p); p += 4;
        p++; // skip "5.x.x\0" - actually read strings
        // easier: known offsets: strings at 12..23 roughly; sizes at 32
        int compInfo = BE32(f, 38);
        int uncompInfo = BE32(f, 42);
        int flags = BE32(f, 46);
        int hdrEnd = 64;
        var rawBi = new byte[compInfo];
        Array.Copy(f, hdrEnd, rawBi, 0, compInfo);
        byte[] info = Lz4BlockDecompress(rawBi, uncompInfo);

        Console.WriteLine($"compInfo={compInfo} uncompInfo={uncompInfo} flags=0x{flags:X}");
        int count = BE32(info, 16);
        Console.WriteLine($"blocks={count}");
        int dirOff = 20 + count * 10;
        int nodes = BE32(info, dirOff);
        Console.WriteLine($"\ndirectory nodes={nodes} at offset 0x{dirOff:X}\n");
        Console.WriteLine($"{"#",-4} {"offset",-12} {"size",-12} {"flags",-10} name");
        int q = dirOff + 4;
        for (int i = 0; i < nodes && q + 20 <= info.Length; i++)
        {
            long off = BE64(info, q);
            long size = BE64(info, q + 8);
            int fl = BE32(info, q + 16);
            int nameStart = q + 20;
            int e = nameStart;
            while (e < info.Length && info[e] != 0) e++;
            string name = Encoding.UTF8.GetString(info, nameStart, e - nameStart);
            q = e + 1;
            // find readable slice in bin
            string sample = "";
            if (off >= 0 && off < bin.Length)
            {
                int n = (int)Math.Min(40, bin.Length - off);
                var seg = new byte[n];
                Buffer.BlockCopy(bin, (int)off, seg, 0, n);
                sample = Encoding.Latin1.GetString(seg).Replace("\0", "·");
            }
            Console.WriteLine($"{i,-4} 0x{off:X8} 0x{size:X8} 0x{fl:X8} {name}  [{sample}]");
        }
    }
}