using System;
using System.IO;
using System.Linq;
using System.Reflection;

string[] searchDirs =
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
        var path = Path.Combine(dir, name + ".dll");
        if (File.Exists(path)) return Assembly.LoadFrom(path);
    }
    return null;
};

string dll = @"D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\core\BepInEx.Unity.IL2CPP.dll";
var asm = Assembly.LoadFrom(dll);
Type[] types;
try { types = asm.GetTypes(); }
catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }

var cl = types.First(t => t.Name == "IL2CPPChainloader");
Console.WriteLine("=== IL2CPPChainloader methods (name contains AddUnity/Unity/Component) ===");
foreach (var m in cl.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                   .Where(m => m.Name.Contains("Unity") || m.Name.Contains("Component") || m.Name.Contains("Add")))
{
    string pars = string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
    Console.WriteLine($"  [{(m.IsStatic ? "static" : "inst")}] {m.Name}({pars}) : {m.ReturnType.Name}");
}

Console.WriteLine("=== AddUnityComponent full signatures (all overloads) ===");
foreach (var m in cl.GetMethods(BindingFlags.Public | BindingFlags.Static)
                   .Where(m => m.Name == "AddUnityComponent"))
{
    string pars = string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
    bool generic = m.IsGenericMethod;
    Console.WriteLine($"  [{(m.IsStatic ? "static" : "inst")}] AddUnityComponent{(generic ? "<" + string.Join(",", m.GetGenericArguments().Select(g => g.Name)) + ">" : "")}({pars}) : {m.ReturnType.Name}  genericMethod={generic}");
}

Console.WriteLine("\nDONE");
