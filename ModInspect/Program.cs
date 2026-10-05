using System;
using System.IO;
using System.Linq;
using Mono.Cecil;

class ModInspect
{
    static void Dump(ModuleDefinition mod, string typeName, int maxMethods = 400)
    {
        var t = mod.Types.FirstOrDefault(x => x.Name == typeName);
        if (t == null) { Console.WriteLine($"  !! type {typeName} not found"); return; }
        Console.WriteLine($"== TYPE {t.FullName}  [{(t.IsValueType ? "struct" : "class")}] base={t.BaseType?.FullName}");
        foreach (var f in t.Fields)
            Console.WriteLine($"   field  {f.FieldType.FullName}  {f.Name}");
        foreach (var p in t.Properties)
            Console.WriteLine($"   prop   {p.PropertyType.FullName}  {p.Name}");
        int n = 0;
        foreach (var m in t.Methods)
        {
            if (n++ > maxMethods) { Console.WriteLine($"   ... ({t.Methods.Count} total)"); break; }
            var pars = string.Join(", ", m.Parameters.Select(p => p.ParameterType.FullName + " " + p.Name));
            Console.WriteLine($"   method {m.Attributes} {(m.ReturnType.FullName)} {m.Name}({pars})");
        }
    }

    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--listtypes")
        {
            string dllL = @"D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\interop\Assembly-CSharp.dll";
            string[] pats = { "Node", "Gather", "Harv", "Mine", "Chop", "Resource", "Cut", "Forage", "Bush", "Plant", "Stone", "Wood", "Log", "Grass", "Crop", "Tree", "Interact", "Loot", "Drop", "Pickaxe", "Tool", "Pickup" };
            var asmL = AssemblyDefinition.ReadAssembly(dllL);
            foreach (var t in asmL.MainModule.Types)
            {
                if (t.IsNested) continue;
                foreach (var pat in pats)
                {
                    if (t.Name.IndexOf(pat, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"TYPE {t.FullName} [{(t.IsValueType ? "struct" : "class")}] base={t.BaseType?.FullName}");
                        break;
                    }
                }
            }
            return;
        }
        if (args.Length > 0 && args[0] == "--scan")
        {
            foreach (var f in Directory.GetFiles(@"D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\core", "BepInEx*.dll"))
            {
                try
                {
                    var a = AssemblyDefinition.ReadAssembly(f);
                    foreach (var t in a.MainModule.Types)
                        if (t.Name == "BasePlugin" || t.Name == "Chainloader" || t.Name == "Plugin")
                            Console.WriteLine($"{Path.GetFileName(f)} :: {t.FullName} (base={t.BaseType?.FullName})");
                }
                catch (Exception e) { Console.WriteLine($"{Path.GetFileName(f)} ERR {e.Message}"); }
            }
            return;
        }

        if (args.Length > 0 && args[0] == "--nested")
        {
            string parent = args[1];
            var asmN = AssemblyDefinition.ReadAssembly(@"D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\interop\Assembly-CSharp.dll");
            var pt = asmN.MainModule.Types.FirstOrDefault(x => x.Name == parent);
            if (pt == null) { Console.WriteLine("parent not found"); return; }
            foreach (var nt in pt.NestedTypes)
            {
                Console.WriteLine($"== NESTED {nt.FullName} [{(nt.IsValueType ? "struct" : "class")}] base={nt.BaseType?.FullName}");
                var vals = nt.Fields.Where(f => f.IsStatic && f.IsLiteral);
                if (nt.IsEnum)
                {
                    foreach (var f in vals)
                        Console.WriteLine($"   enum {f.Name} = {f.Constant}");
                }
                else
                {
                    foreach (var f in nt.Fields) Console.WriteLine($"   field  {f.FieldType.FullName}  {f.Name}");
                }
                foreach (var m in nt.Methods.Where(m => !m.IsSpecialName).Take(50))
                    Console.WriteLine($"   method {m.ReturnType.FullName} {m.Name}({string.Join(", ", m.Parameters.Select(p => p.ParameterType.FullName))})");
            }
            return;
        }

        string dll = args.Length > 0 && (args[0].Contains("\\") || args[0].Contains(".dll")) ? args[0]
            : @"D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\interop\Assembly-CSharp.dll";
        Console.WriteLine("Assembly: " + dll);
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(dll)));
        resolver.AddSearchDirectory(@"D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\interop");
        resolver.AddSearchDirectory(@"D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\unity-libs");
        var rp = new ReaderParameters { AssemblyResolver = resolver };
        var asm = AssemblyDefinition.ReadAssembly(dll, rp);
        var mod = asm.MainModule;
        string[] targets = { "RelationshipData", "NPCDatabase", "NPCDialogues", "PlayerDialogueEntry" };
        if (args.Length > 0)
        {
            targets = args.Length > 1 ? args.Skip(1).ToArray() : args.Skip(0).ToArray();
            if (targets.Length == 1 && targets[0] == "all")
                targets = new[] { "Harvest", "HarvestClient", "InteractClient", "HarvestOption", "HarvestedItem", "ChanceHarvestedItem", "HarvestType", "SceneHarvestable", "InteractManager", "ItemDrop", "ItemDropGroup", "ChanceItemDrop", "GoldDropPickup", "HarvestSpawn", "InteractableReward" };
        }
        foreach (var tn in targets) Dump(mod, tn);
    }
}
