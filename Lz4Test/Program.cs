using System;
using System.IO;

class Lz4Test
{
    static byte[] Lz4BlockDecompress(byte[] src, int uncompressedSize)
    {
        byte[] dst = new byte[uncompressedSize];
        int sp = 0, dp = 0;
        while (sp < src.Length && dp < dst.Length)
        {
            int token = src[sp++];
            int litLen = token >> 4;
            if (litLen == 15)
            {
                byte b;
                do { b = src[sp++]; litLen += b; } while (b == 255);
            }
            Array.Copy(src, sp, dst, dp, litLen);
            sp += litLen; dp += litLen;
            if (dp >= dst.Length) break;
            if (sp + 2 > src.Length) break;
            int offset = src[sp] | (src[sp + 1] << 8);
            sp += 2;
            if (offset == 0) break;
            int matchLen = token & 0xF;
            if (matchLen == 15)
            {
                byte b;
                do { b = src[sp++]; matchLen += b; } while (b == 255);
            }
            matchLen += 4;
            for (int i = 0; i < matchLen; i++)
            {
                dst[dp] = dst[dp - offset];
                dp++;
            }
        }
        return dst;
    }

    static void Main()
    {
        string path = @"D:\SteamLibrary\steamapps\common\Dimraeth\Dimraeth_Data\StreamingAssets\aa\StandaloneWindows64\loadingscreen_sprites_assets_all_98a28461a1fc6503a5ae5d1cf8611445.bundle";
        var f = File.ReadAllBytes(path);
        // block info at 0x40 (64), 147 bytes, decompress to 263
        byte[] info = new byte[147];
        Array.Copy(f, 64, info, 0, 147);
        Console.WriteLine("raw hex: " + BitConverter.ToString(info).Replace("-", " "));
        var dec = Lz4BlockDecompress(info, 263);
        Console.WriteLine("dec len: " + dec.Length);
        Console.WriteLine("dec hex: " + BitConverter.ToString(dec).Replace("-", " "));
        for (int i = 0; i < 263; i += 16)
        {
            int n = Math.Min(16, 263 - i);
            var seg = new byte[n];
            Array.Copy(dec, i, seg, 0, n);
            Console.WriteLine($"{i:X4}: {BitConverter.ToString(seg).Replace("-", " ")}");
        }
    }
}