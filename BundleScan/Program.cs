using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class BundleScan
{
    // LZ4 raw block decompression (no magic bytes, no frame).
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
            if (sp + litLen > src.Length) break;
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
            for (int i = 0; i < matchLen && dp < dst.Length; i++)
            {
                dst[dp] = dst[dp - offset];
                dp++;
            }
        }
        return dst;
    }

    static int BE32(byte[] b, int p) =>
        (b[p] << 24) | (b[p + 1] << 16) | (b[p + 2] << 8) | b[p + 3];
    static long BE64(byte[] b, int p) =>
        ((long)BE32(b, p) << 32) | (uint)BE32(b, p + 4);

    static string ReadCString(byte[] b, ref int p)
    {
        int start = p;
        while (p < b.Length && b[p] != 0) p++;
        string s = Encoding.ASCII.GetString(b, start, p - start);
        p++;
        return s;
    }

    class UFSHeader
    {
        public int version;
        public string unityRevision;
        public long fileSize;
        public int compInfoSize;
        public int uncompInfoSize;
        public int flags;
        public int headerEnd;
    }

    static (UFSHeader h, string err) TryParseHeader(byte[] f)
    {
        if (f.Length < 64 || Encoding.ASCII.GetString(f, 0, 7) != "UnityFS")
            return (null, "not UnityFS");
        int p = 8;
        int version = BE32(f, p); p += 4;
        string uv = ReadCString(f, ref p);
        string ur = ReadCString(f, ref p);
        long fileSize = version >= 7 ? BE64(f, p) : BE32(f, p);
        if (version >= 7) p += 8; else p += 4;
        int compInfo = BE32(f, p); p += 4;
        int uncompInfo = BE32(f, p); p += 4;
        int flags = BE32(f, p); p += 4;
        // align to 16 for version >= 7
        int headerEnd = p >= 7 ? (p + 15) & ~15 : p;
        // sanity: headerEnd + compInfo must fit
        return (new UFSHeader { version = version, unityRevision = ur, fileSize = fileSize,
            compInfoSize = compInfo, uncompInfoSize = uncompInfo, flags = flags, headerEnd = headerEnd }, null);
    }

    static (List<(int u, int c, int fl)> blocks, byte[] info, string err) ParseBlockInfo(byte[] raw, int compSize, int uncompSize, int flags)
    {
        byte[] info;
        int compMode = flags & 0x3F;
        if (compMode == 2 || compMode == 3)
        {
            info = Lz4BlockDecompress(raw, uncompSize);
        }
        else if (compMode == 1)
        {
            return (null, null, "LZMA block info");
        }
        else
        {
            info = raw;
        }
        if (info.Length < 20) return (null, info, "too short");
        int count = BE32(info, 16);
        if (count <= 0 || count > 1_000_000) return (null, info, $"bad count {count}");
        var blocks = new List<(int, int, int)>();
        for (int i = 0; i < count; i++)
        {
            int off = 20 + i * 10;
            if (off + 10 > info.Length) return (null, info, $"block {i} out of range");
            int u = BE32(info, off);
            int c = BE32(info, off + 4);
            int fl = (info[off + 8] << 8) | info[off + 9];
            blocks.Add((u, c, fl));
        }
        return (blocks, info, null);
    }

    static void Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0]
            : @"D:\SteamLibrary\steamapps\common\Dimraeth\Dimraeth_Data\StreamingAssets\aa\StandaloneWindows64";
        string[] markers = { "HeroicOption", "ScholarOption", "RelationshipBonus", "PlayerDialogueEntry",
            "EntryGUID", "StoicOption", "RelationshipData", "NPCResponseID", "LocalizedPlayerOption",
            "JokerOption", "DialogueText", "GetLocalizedPlayerOption" };
        int statsOk = 0, statsLzma = 0, statsOther = 0;
        foreach (var fi in Directory.GetFiles(dir, "*.bundle"))
        {
            try
            {
                var f = File.ReadAllBytes(fi);
                var (h, err) = TryParseHeader(f);
                if (h == null) { statsOther++; Console.WriteLine($"{Path.GetFileName(fi)}: {err}"); continue; }
                Console.WriteLine($"\n{Path.GetFileName(fi)} (len={f.Length}): ver={h.version} unity={h.unityRevision} size={h.fileSize} compInfo={h.compInfoSize} uncompInfo={h.uncompInfoSize} flags=0x{h.flags:X} hdrEnd={h.headerEnd}");
                if (h.fileSize != f.Length)
                    Console.WriteLine($"   WARN: header fileSize {h.fileSize} != actual {f.Length}");

                int blkStart = h.headerEnd;
                if (blkStart + h.compInfoSize > f.Length)
                {
                    // maybe block info at end (flags & 0x80)
                    blkStart = (int)f.Length - h.compInfoSize;
                    Console.WriteLine($"   block info at END? blkStart={blkStart}");
                }
                var rawBi = new byte[h.compInfoSize];
                Array.Copy(f, blkStart, rawBi, 0, h.compInfoSize);
                var (blocks, bi, berr) = ParseBlockInfo(rawBi, h.compInfoSize, h.uncompInfoSize, h.flags);
                if (blocks == null)
                {
                    Console.WriteLine($"   FAIL block info: {berr}");
                    statsOther++; continue;
                }
                long sumU = 0, sumC = 0;
                bool hasLzma = false;
                foreach (var b in blocks) { sumU += b.u; sumC += b.c; if (b.fl == 1) hasLzma = true; }
                Console.WriteLine($"   blocks={blocks.Count} sumU={sumU} sumC={sumC}");

                long dataStart = blkStart + h.compInfoSize;
                // padding for block data start if flag 0x200
                if ((h.flags & 0x200) != 0) dataStart = (dataStart + 15) & ~15;
                Console.WriteLine($"   dataStart={dataStart} expectedEnd={dataStart + sumC} fileLen={f.Length}");
                if (dataStart + sumC > f.Length)
                {
                    Console.WriteLine($"   FAIL: block data overruns file");
                    statsOther++; continue;
                }
                if (hasLzma) { statsLzma++; Console.WriteLine($"   LZMA block present - skip"); continue; }

                string rawFn = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(fi) + ".raw");
                long totalWritten = 0;
                int p = (int)dataStart;
                long bad = 0;
                using (var fs = new FileStream(rawFn, FileMode.Create, FileAccess.Write))
                {
                    foreach (var b in blocks)
                    {
                        var comp = new byte[b.c];
                        Array.Copy(f, p, comp, 0, b.c);
                        p += b.c;
                        byte[] raw;
                        if (b.fl == 2 || b.fl == 3) raw = Lz4BlockDecompress(comp, b.u);
                        else if (b.fl == 0) raw = comp;
                        else { bad++; continue; }
                        fs.Write(raw, 0, raw.Length);
                        totalWritten += raw.Length;
                    }
                }
                if (bad > 0) Console.WriteLine($"   WARN {bad} skipped blocks");
                Console.WriteLine($"   decompressed {totalWritten} bytes");

                string outFn = Path.Combine(Path.GetDirectoryName(fi),
                    Path.GetFileNameWithoutExtension(fi) + ".decompressed.bin");
                File.Copy(rawFn, outFn, true);
                Console.WriteLine($"   saved -> {outFn}");

                // chunk-scan UTF8 for markers
                var hits = new Dictionary<string, int>();
                int chunk = 16 * 1024 * 1024;
                var contexts = new Dictionary<string, string>();
                long leftover = 0;
                byte[] prev = Array.Empty<byte>();
                using (var fs = new FileStream(rawFn, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    while (fs.Position < fs.Length)
                    {
                        int n = (int)Math.Min(chunk, fs.Length - fs.Position);
                        var seg = new byte[n];
                        br.Read(seg, 0, n);
                        var joined = new byte[prev.Length + seg.Length];
                        Buffer.BlockCopy(prev, 0, joined, 0, prev.Length);
                        Buffer.BlockCopy(seg, 0, joined, prev.Length, seg.Length);
                        foreach (var m in markers)
                        {
                            int cnt = CountBytes(joined, m);
                            if (cnt > 0)
                            {
                                hits[m] = hits.GetValueOrDefault(m) + cnt;
                                if (!contexts.ContainsKey(m))
                                {
                                    int idx = IndexOfAny(joined, m);
                                    int s = Math.Max(0, idx - 150), l = Math.Min(700, joined.Length - s);
                                    var segS = new byte[l];
                                    Buffer.BlockCopy(joined, s, segS, 0, l);
                                    contexts[m] = Encoding.UTF8.GetString(segS);
                                }
                            }
                        }
                        // carry last (maxMarkerLen-1) bytes for cross-boundary matches
                        int carry = 24;
                        prev = new byte[carry];
                        int cp = Math.Max(0, joined.Length - carry);
                        Buffer.BlockCopy(joined, cp, prev, 0, joined.Length - cp);
                    }
                }
                File.Delete(rawFn);
                Console.WriteLine($"   hits: {(hits.Count == 0 ? "none" : string.Join(" ", hits.Select(kv => $"{kv.Key}={kv.Value}")))}");
                if (hits.Count > 0)
                {
                    statsOk++;
                    foreach (var m in new[] { "RelationshipBonus", "HeroicOption", "ScholarOption" })
                    {
                        if (contexts.TryGetValue(m, out var ctx))
                        {
                            Console.WriteLine($"  --- ctx [{m}] ---");
                            Console.WriteLine(Sanitize(ctx));
                        }
                    }
                }
                else statsOther++;
            }
            catch (Exception ex)
            {
                statsOther++;
                Console.WriteLine($"{Path.GetFileName(fi)}: EX {ex.Message}");
            }
        }
        Console.WriteLine($"\nSummary: hits={statsOk} lzma={statsLzma} other={statsOther}");
    }

    static int CountBytes(byte[] data, string needle)
    {
        var nb = Encoding.UTF8.GetBytes(needle);
        int cnt = 0;
        int limit = data.Length - nb.Length;
        for (int i = 0; i <= limit; i++)
        {
            bool ok = true;
            for (int j = 0; j < nb.Length; j++)
                if (data[i + j] != nb[j]) { ok = false; break; }
            if (ok) { cnt++; i += nb.Length - 1; }
        }
        return cnt;
    }

    static int IndexOfAny(byte[] data, string needle)
    {
        var nb = Encoding.UTF8.GetBytes(needle);
        for (int i = 0; i <= data.Length - nb.Length; i++)
        {
            bool ok = true;
            for (int j = 0; j < nb.Length; j++)
                if (data[i + j] != nb[j]) { ok = false; break; }
            if (ok) return i;
        }
        return -1;
    }

    static int Count(string s, string needle)
    {
        int cnt = 0, i0 = 0;
        while ((i0 = s.IndexOf(needle, i0, StringComparison.Ordinal)) >= 0) { cnt++; i0 += needle.Length; }
        return cnt;
    }

    static string Sanitize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char c in s) sb.Append(char.IsControl(c) ? ' ' : c);
        return sb.ToString();
    }
}