using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class SerDump
{
    static int I32(byte[] b, int p, bool be) => be ? (b[p]<<24)|(b[p+1]<<16)|(b[p+2]<<8)|b[p+3] : b[p]|(b[p+1]<<8)|(b[p+2]<<16)|(b[p+3]<<24);
    static long I64(byte[] b, int p, bool be)
    {
        if (be) return ((long)I32(b,p,true)<<32)|(uint)I32(b,p+4,true);
        return (long)(uint)I32(b,p,false)|((long)I32(b,p+4,false)<<32);
    }
    static string CStr(byte[] b, ref int p)
    {
        int s = p; while (p < b.Length && b[p] != 0) p++;
        var r = Encoding.UTF8.GetString(b, s, p - s);
        p++;
        return r;
    }
    static int Align4(int p) => (p + 3) & ~3;

    static int SkipTypeTreeBlob(byte[] f, int p, int version, bool be)
    {
        int nodeCount = I32(f, p, be); p += 4;
        int strBufSize = I32(f, p, be); p += 4;
        int nodeSize = version >= 19 ? 32 : 24;
        p += nodeCount * nodeSize;
        p += strBufSize;
        return p;
    }
    static int SkipRefType(byte[] f, int p, int version, bool be, bool typeTree)
    {
        int classId = I32(f, p, be); p += 4;
        if (version >= 16) p++;
        if (version >= 17) p += 2;
        if (version >= 13) p += 32;
        if (typeTree) p = SkipTypeTreeBlob(f, p, version, be);
        if (21 <= version && version < 22) { int deps = I32(f,p,be); p+=4; p += deps*4; }
        return p;
    }

    static void Parse(string path)
    {
        long len = new FileInfo(path).Length;
        Console.WriteLine($"\n########## {Path.GetFileName(path)} ({len:n0}) ##########");
        int serLen = (int)Math.Min(len, int.MaxValue);
        var f = new byte[serLen];
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            fs.Read(f, 0, serLen);
        int p = 0;
        int m0 = I32(f, p, true); p += 4;
        int fileSize0 = I32(f, p, true); p += 4;
        int version = I32(f, p, true); p += 4;
        int dataOff0 = I32(f, p, true); p += 4;
        if (version < 5 || version > 30) { Console.WriteLine("  not a valid serialized header"); return; }
        bool dataBe = f[p] == 1; p++;
        p += 3;
        int metadataSize = I32(f, p, true); p += 4;
        long fileSize = I64(f, p, true); p += 8;
        long dataOffset = I64(f, p, true); p += 8;
        long unknown = I64(f, p, true); p += 8;
        Console.WriteLine($"  ver={version} data_be={dataBe} metadata={metadataSize} fileSize={fileSize} dataOffset={dataOffset}");
        if (version >= 7) { string uv = CStr(f, ref p); Console.WriteLine($"  unity: {uv}"); }
        int targetPlatform = 0;
        if (version >= 8) { targetPlatform = I32(f, p, dataBe); p += 4; }
        bool typeTree = false;
        if (version >= 13) { typeTree = f[p] == 1; p++; }

        int typeCount = I32(f, p, dataBe); p += 4;
        var types = new List<(int idx, int classId)>();
        for (int i = 0; i < typeCount; i++)
        {
            if (p + 4 > f.Length) { Console.WriteLine($"  TYPE LOOP BREAK at i={i} p=0x{p:X} (buf {f.Length})"); break; }
            int classId = I32(f, p, dataBe); p += 4;
            bool stripped = false;
            if (version >= 16) { stripped = f[p] == 1; p++; }
            int sti = -1;
            if (version >= 17) { sti = (short)(f[p] | (f[p+1]<<8)); p += 2; }
            if (version >= 13) p += 16; // old_type_hash (only; no separate script_id)
            if (typeTree) p = SkipTypeTreeBlob(f, p, version, dataBe);
            if (21 <= version && version < 22) { int deps = I32(f,p,dataBe); p+=4; p += deps*4; }
            types.Add((i, classId));
        }
        Console.WriteLine($"  types={typeCount} classes: {string.Join(",", types.Select(t=>t.classId).Distinct().OrderBy(x=>x))}");
        bool hasText = types.Any(t => t.classId == 49);
        Console.WriteLine($"  has TextAsset(49): {hasText}");

        if (7 <= version && version < 14) { p += 4; }
        int objectCount = I32(f, p, dataBe); p += 4;
        var classCounts = new Dictionary<int,int>();
        var textAssets = new List<(long pathId, long start, int size)>();
        for (int i = 0; i < objectCount; i++)
        {
            if (version >= 14) p = Align4(p);
            long pathId;
            if (version >= 14) pathId = I64(f, p, dataBe); else pathId = I32(f, p, dataBe);
            p += version >= 14 ? 8 : 4;
            long byteStart;
            if (version >= 22) { byteStart = I64(f, p, dataBe); p += 8; }
            else { byteStart = (uint)I32(f, p, dataBe); p += 4; }
            byteStart += dataOffset;
            int byteSize = (int)(uint)I32(f, p, dataBe); p += 4;
            int typeId = I32(f, p, dataBe); p += 4;
            var t = types.FirstOrDefault(x => x.idx == typeId);
            int classId = t.classId;
            classCounts[classId] = classCounts.GetValueOrDefault(classId) + 1;
            if (classId == 49) textAssets.Add((pathId, byteStart, byteSize));
        }
        Console.WriteLine($"  objects={objectCount}");
        Console.WriteLine($"  class counts: {string.Join(", ", classCounts.OrderBy(k=>k.Key).Select(k=>$"{k.Key}={k.Value}"))}");
        Console.WriteLine($"  TextAssets found: {textAssets.Count}");
        foreach (var ta in textAssets)
        {
            string name = ReadStr(f, ta.start, dataBe);
            var script = ReadBlob(f, ta.start, dataBe);
            Console.WriteLine($"    [{ta.pathId}] name=\"{name}\" scriptLen={script.Length}");
            Console.WriteLine($"        preview: {Preview(script, 300)}");
        }
    }

    static string ReadStr(byte[] f, long start, bool be)
    {
        int p = (int)start; if (p < 0 || p + 4 > f.Length) return "?";
        int len = I32(f, p, be); p += 4;
        if (len < 0 || p + len > f.Length) return "?";
        return Encoding.UTF8.GetString(f, p, len);
    }
    static byte[] ReadBlob(byte[] f, long start, bool be)
    {
        int p = (int)start; if (p < 0 || p + 4 > f.Length) return Array.Empty<byte>();
        int nameLen = I32(f, p, be); p += 4;
        p += Math.Max(0, nameLen);
        p = Align4(p);
        if (p + 4 > f.Length) return Array.Empty<byte>();
        int len = I32(f, p, be); p += 4;
        if (len < 0 || p + len > f.Length) return Array.Empty<byte>();
        var r = new byte[len];
        Array.Copy(f, p, r, 0, len);
        return r;
    }
    static string Preview(byte[] s, int max)
    {
        var txt = Encoding.UTF8.GetString(s).Replace("\r","\\r").Replace("\n","\\n");
        if (txt.Length > max) txt = txt.Substring(0, max) + "...";
        return txt;
    }

    static void Main(string[] args)
    {
        var files = new List<string> {
            @"D:\SteamLibrary\steamapps\common\Dimraeth\Dimraeth_Data\resources.assets",
            @"D:\SteamLibrary\steamapps\common\Dimraeth\Dimraeth_Data\globalgamemanagers.assets",
            @"D:\SteamLibrary\steamapps\common\Dimraeth\Dimraeth_Data\sharedassets0.assets",
            @"D:\SteamLibrary\steamapps\common\Dimraeth\Dimraeth_Data\sharedassets2.assets",
            @"D:\SteamLibrary\steamapps\common\Dimraeth\Dimraeth_Data\sharedassets3.assets",
        };
        foreach (var f in files)
            if (File.Exists(f)) Parse(f);
    }
}
