using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class SerParse
{
    // ---- binary helpers (endian-aware; assume little-endian data, header has endian flag) ----
    static int I32(byte[] b, int p, bool dataBe) => dataBe ? (b[p]<<24)|(b[p+1]<<16)|(b[p+2]<<8)|b[p+3] : b[p]|(b[p+1]<<8)|(b[p+2]<<16)|(b[p+3]<<24);
    static long I64(byte[] b, int p, bool dataBe)
    {
        if (dataBe) return ((long)I32(b,p,true)<<32)|(uint)I32(b,p+4,true);
        return (long)(uint)I32(b,p,false)|((long)I32(b,p+4,false)<<32);
    }
    static string CStr(byte[] b, ref int p)
    {
        int s = p; while (p < b.Length && b[p] != 0) p++;
        var r = Encoding.UTF8.GetString(b, s, p - s);
        p++;
        return r;
    }

    class TypeInfo { public int index; public int classId; public int scriptTypeIndex; }

    static void Main(string[] args)
    {
        string path = args.Length > 0 ? args[0]
            : @"D:\SteamLibrary\steamapps\common\Dimraeth\Dimraeth_Data\StreamingAssets\aa\StandaloneWindows64\defaultlocalgroup_assets_all_11aa88868962abe284c06cb1ddb7ee3a.decompressed.bin";
        int serLen = args.Length > 1 ? int.Parse(args[1]) : 21512172; // serialized file region size
        var f = new byte[serLen];
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            fs.Read(f, 0, serLen);
        Console.WriteLine($"serialized region len {serLen}");
        int p = 0;
        // header (v22): fixed 4 u32 BIG-endian + endian byte + reserved, then real fields BIG-endian
        int m0 = I32(f, p, true); p += 4;
        int fileSize0 = I32(f, p, true); p += 4;
        int version = I32(f, p, true); p += 4;
        int dataOff0 = I32(f, p, true); p += 4;
        bool dataBe = f[p] == 1; p++; // endianness of metadata/data (1 = big)
        p += 3; // reserved
        int metadataSize = I32(f, p, true); p += 4;
        long fileSize = I64(f, p, true); p += 8;
        long dataOffset = I64(f, p, true); p += 8;
        long unknown = I64(f, p, true); p += 8;
        Console.WriteLine($"m0={m0} fileSize0={fileSize0} version={version} dataOff0={dataOff0} data_endian_big={dataBe} metadataSize={metadataSize} fileSize={fileSize} dataOffset={dataOffset} unknown={unknown}");
        if (fileSize != serLen) Console.WriteLine($"  WARN fileSize {fileSize} != serLen {serLen}");
        if (version >= 7) { string uv = CStr(f, ref p); Console.WriteLine($"  unityVersion: {uv}"); }
        int targetPlatform = 0;
        if (version >= 8) { targetPlatform = I32(f, p, dataBe); p += 4; Console.WriteLine($"  targetPlatform: {targetPlatform}"); }
        bool typeTree = false;
        if (version >= 13) { typeTree = f[p] == 1; p++; Console.WriteLine($"  enableTypeTree: {typeTree}"); }

        // ---- ReadTypes ----
        int typeCount = I32(f, p, dataBe); p += 4;
        Console.WriteLine($"  typeCount: {typeCount}");
        var types = new List<TypeInfo>();
        var scriptIds = new Dictionary<int, byte[]>();
        for (int i = 0; i < typeCount; i++)
        {
            int classId = I32(f, p, dataBe); p += 4;
            bool stripped = false;
            if (version >= 16) { stripped = f[p] == 1; p++; }
            int sti = -1;
            if (version >= 17) { sti = (short)I32(f, p, dataBe); p += 2; }
            if (version >= 13)
            {
                bool readScript = (sti >= 0) || (version < 16 && classId < 0) || (version >= 16 && classId == 114);
                if (readScript) { var sid = new byte[16]; Array.Copy(f, p, sid, 0, 16); scriptIds[classId] = sid; p += 16; }
                p += 16; // old_type_hash
            }
            if (typeTree)
            {
                p = SkipTypeTreeBlob(f, p, version, dataBe);
            }
            if (version >= 21)
            {
                int deps = I32(f, p, dataBe); p += 4;
                p += deps * 4;
            }
            types.Add(new TypeInfo { index = i, classId = classId, scriptTypeIndex = sti });
        }
        Console.WriteLine($"  parsed {types.Count} types; classIDs: {string.Join(",", types.Select(t => t.classId).Distinct().OrderBy(x=>x))}");
        Console.WriteLine($"  has TextAsset(49): {types.Any(t => t.classId == 49)}");

        // ---- big_id_enabled (v7-13 only) ----
        if (7 <= version && version < 14) { int b = I32(f, p, dataBe); p += 4; Console.WriteLine($"  bigIdEnabled: {b}"); }

        // ---- ReadObjects ----
        int objectCount = I32(f, p, dataBe); p += 4;
        Console.WriteLine($"  objectCount: {objectCount}");
        var objs = new List<(long pathId, long byteStart, int byteSize, int typeId)>();
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
            objs.Add((pathId, byteStart, byteSize, typeId));
        }
        Console.WriteLine($"  objects parsed: {objs.Count}");

        // ---- scripts / externals / ref_types / userInfo (skip) ----
        if (version >= 11) { int sc = I32(f, p, dataBe); p += 4; for (int i=0;i<sc;i++){ int lf = I32(f,p,dataBe); p+=4; if (version>=14) p=Align4(p); p+=8; } }
        int extCount = I32(f, p, dataBe); p += 4;
        for (int i=0;i<extCount;i++){
            if (version >= 6) { string t = CStr(f, ref p); }
            if (version >= 5) { p += 16; p += 4; }
            string path2 = CStr(f, ref p);
            Console.WriteLine($"    external: {path2}");
        }
        if (version >= 20) { int rc = I32(f, p, dataBe); p += 4; for (int i=0;i<rc;i++){ p = SkipRefType(f, p, version, dataBe, typeTree); } }
        if (version >= 5) { string ui = CStr(f, ref p); }
        Console.WriteLine($"  metadata end p=0x{p:X}, dataOffset=0x{dataOffset:X}");

        // ---- dump TextAsset objects ----
        var textAssets = new List<(long pathId, long start, int size)>();
        foreach (var o in objs)
        {
            var t = types.FirstOrDefault(x => x.index == o.typeId);
            int classId = t == null ? -1 : t.classId;
            if (classId == 49) textAssets.Add((o.pathId, o.byteStart, o.byteSize));
        }
        Console.WriteLine($"\nTextAsset objects: {textAssets.Count}");
        int dumpCount = 0;
        foreach (var ta in textAssets)
        {
            if (dumpCount >= 20) break;
            var name = ReadTextAssetName(f, ta.start, dataBe);
            var script = ReadTextAssetScript(f, ta.start, dataBe);
            Console.WriteLine($"\n=== TextAsset pathId={ta.pathId} size={ta.size} name=\"{name}\" ===");
            Console.WriteLine($"   script len {script.Length}");
            Console.WriteLine($"   preview: {Preview(script, 500)}");
            dumpCount++;
        }
    }

    static string Preview(byte[] s, int max)
    {
        var txt = Encoding.UTF8.GetString(s).Replace("\r", "\\r").Replace("\n", "\\n");
        if (txt.Length > max) txt = txt.Substring(0, max) + "...";
        return txt;
    }

    // TextAsset: m_Name (string: int32 len + bytes + align4), then m_Script (int32 len + bytes)
    static string ReadTextAssetName(byte[] f, long start, bool dataBe)
    {
        int p = (int)start;
        int len = I32(f, p, dataBe); p += 4;
        if (len < 0 || p + len > f.Length) return "?";
        string s = Encoding.UTF8.GetString(f, p, len);
        return s;
    }
    static byte[] ReadTextAssetScript(byte[] f, long start, bool dataBe)
    {
        int p = (int)start;
        int nameLen = I32(f, p, dataBe); p += 4;
        p += nameLen;
        p = Align4(p);
        int len = I32(f, p, dataBe); p += 4;
        if (len < 0 || p + len > f.Length) return Array.Empty<byte>();
        var r = new byte[len];
        Array.Copy(f, p, r, 0, len);
        return r;
    }

    static int Align4(int p) => (p + 3) & ~3;

    static int SkipTypeTreeBlob(byte[] f, int p, int version, bool dataBe)
    {
        int nodeCount = I32(f, p, dataBe); p += 4;
        int strBufSize = I32(f, p, dataBe); p += 4;
        int nodeSize = version >= 19 ? 32 : 24;
        p += nodeCount * nodeSize;
        p += strBufSize;
        return p;
    }

    static int SkipRefType(byte[] f, int p, int version, bool dataBe, bool typeTree)
    {
        int classId = I32(f, p, dataBe); p += 4;
        if (version >= 16) p++; // stripped
        if (version >= 17) p += 2; // sti
        if (version >= 13) { p += 32; } // script_id + old_type_hash (assume max)
        if (typeTree) p = SkipTypeTreeBlob(f, p, version, dataBe);
        if (version >= 21) { int deps = I32(f,p,dataBe); p+=4; p += deps*4; }
        return p;
    }
}
