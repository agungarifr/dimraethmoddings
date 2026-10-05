using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

class CheckRva
{
    static void Main(string[] args)
    {
        string dll = @"D:\SteamLibrary\steamapps\common\Dimraeth\modding\MelonLoader\Dependencies\Il2CppAssemblyGenerator\Cpp2IL\cpp2il_out\Assembly-CSharp.dll";
        if (!File.Exists(dll)) { Console.WriteLine("missing " + dll); return; }
        using var fs = File.OpenRead(dll);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();

        var wantTypes = new HashSet<string>(new[] { "Runes", "Forge", "RuneManager", "ForgePanelView", "ForgePanelPreviewPresenter" });
        foreach (var tdh in md.TypeDefinitions)
        {
            var td = md.GetTypeDefinition(tdh);
            string name = md.GetString(td.Name);
            if (!wantTypes.Contains(name)) continue;
            string ns = md.GetString(td.Namespace);
            Console.WriteLine($"\n== TYPE {ns}.{name}");
            foreach (var mh in td.GetMethods())
            {
                var m = md.GetMethodDefinition(mh);
                uint rva = (uint)m.RelativeVirtualAddress;
                string mname = md.GetString(m.Name);
                Console.WriteLine($"   {mname,-45} RVA=0x{rva:X8}");
            }
        }
    }
}