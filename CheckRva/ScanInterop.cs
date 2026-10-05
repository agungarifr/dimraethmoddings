using System;
using System.IO;
using System.Linq;
using System.Reflection;

class ScanInterop
{
    static void Main()
    {
        var searchDirs = new[]
        {
            @"D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\interop",
            @"D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\core",
            @"D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\dotnet",
        };
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var name = new AssemblyName(e.Name).Name;
            foreach (var dir in searchDirs)
            {
                var p = Path.Combine(dir, name + ".dll");
                if (File.Exists(p)) { try { return Assembly.LoadFrom(p); } catch { } }
            }
            return null;
        };

        var dll = @"D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\interop\Assembly-CSharp.dll";
        var asm = Assembly.LoadFrom(dll);
        string[] wantTypes = { "Player", "PlayerStats", "NPCData", "NPCInteractions", "CharacterCreation", "NPCDatabase", "ConversationArchetypeData", "Persona" };

        string[] patterns = { "Archetype", "Persona", "Conversation", "Relationship", "Dialogue", "Option", "Opinion", "Friend", "Romance", "ValueAlign" };

        foreach (var want in wantTypes)
        {
            var t = asm.GetTypes().FirstOrDefault(x => x.Name == want);
            Console.WriteLine("\n=================== " + want + " ===================");
            if (t == null) { Console.WriteLine("  [NOT FOUND]"); continue; }
            Console.WriteLine("Fields:");
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                               .Where(f => patterns.Any(p => f.Name.Contains(p, StringComparison.OrdinalIgnoreCase))))
            {
                string tn = "?"; try { tn = f.FieldType.Name; } catch { }
                Console.WriteLine($"    {f.Name} : {tn} {(f.IsStatic ? "static" : "")}");
            }
            Console.WriteLine("Props:");
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                               .Where(p => patterns.Any(pat => p.Name.Contains(pat, StringComparison.OrdinalIgnoreCase))))
            {
                string tn = "?"; try { tn = p.PropertyType.Name; } catch { }
                Console.WriteLine($"    {p.Name} : {tn}");
            }
            Console.WriteLine("Methods:");
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                               .Where(m => patterns.Any(pat => m.Name.Contains(pat, StringComparison.OrdinalIgnoreCase)))
                               .OrderBy(m => m.Name))
            {
                string pars = "?"; try { pars = string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)); } catch { }
                Console.WriteLine($"    {(m.IsStatic ? "static " : "")}{m.Name}({pars}) : {(m.ReturnType.Name ?? "?")}");
            }
        }
    }
}