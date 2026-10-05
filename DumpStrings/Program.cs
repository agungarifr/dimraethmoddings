using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

class DumpStrings
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

        string[] typeNames = {
            "NPCDatabase", "PlayerDialogueEntry", "GreetingEntry", "SpecificTopicEntry",
            "ConversationArchetypeData", "ConversationArchetypeUtils", "DialogueOption", "DialogueChoice"
        };

        foreach (var tn in typeNames)
        {
            var t = asm.GetTypes().FirstOrDefault(x => x.Name == tn);
            Console.WriteLine("\n=================== " + tn + " ===================");
            if (t == null) { Console.WriteLine("  [NOT FOUND]"); continue; }

            var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var m in methods)
            {
                byte[] il;
                try { il = m.GetMethodBody()?.GetILAsByteArray(); }
                catch { continue; }
                if (il == null || il.Length == 0) continue;
                var strings = ExtractLdstr(il, m.Module);
                if (strings.Count == 0) continue;
                Console.WriteLine($"  --- {m.Name} ({il.Length} bytes IL) ---");
                foreach (var s in strings)
                    Console.WriteLine($"      \"{Sanitize(s)}\"");
            }
        }
    }

    static List<string> ExtractLdstr(byte[] il, Module mod)
    {
        var res = new List<string>();
        int i = 0;
        while (i < il.Length)
        {
            byte op = il[i];
            if (op == 0x72) // ldstr <token>
            {
                int tok = BitConverter.ToInt32(il, i + 1);
                try
                {
                    string s = mod.ResolveString(tok);
                    if (s != null) res.Add(s);
                }
                catch { }
                i += 5;
            }
            else
            {
                // skip based on prefix/size heuristics
                int len = InstrLen(il, i);
                i += Math.Max(1, len);
            }
        }
        return res;
    }

    static int InstrLen(byte[] il, int i)
    {
        byte op = il[i];
        if (op == 0xFE) return 3; // two-byte opcode + operand guess
        // operand sizes (rough)
        if (op >= 0x00 && op <= 0x05) return 1; // nop, break, ldarg.0..3
        if (op >= 0x06 && op <= 0x0D) return 2; // ldloc.0..3, stloc.0..3 -> actually these are 1 byte; overestimate ok
        if (op >= 0x0E && op <= 0x12) return 2;
        if (op >= 0x13 && op <= 0x16) return 5; // ldc.i4
        if (op >= 0x17 && op <= 0x1E) return 1;
        if (op >= 0x1F && op <= 0x24) return 2;
        if (op == 0x25 || op == 0x26) return 1; // dup, pop
        if (op >= 0x27 && op <= 0x2A) return 1; // jmp,call,calli,ret -> jmp/call/calli are 5, ret 1
        if (op == 0x28 || op == 0x29 || op == 0x2A) return 5;
        if (op >= 0x2B && op <= 0x34) return 2; // br, brfalse etc short -> actually 2
        if (op >= 0x35 && op <= 0x3E) return 5;
        if (op >= 0x3F && op <= 0x44) return 2;
        if (op >= 0x45 && op <= 0x46) return 1; // newarr? no 0x46 newarr is 5
        if (op == 0x72) return 5;
        if (op >= 0x73 && op <= 0x75) return 5; // newobj, castclass, isinst
        if (op == 0x76) return 1; // neg
        if (op >= 0x77 && op <= 0x7E) return 1;
        if (op >= 0x7F && op <= 0x81) return 1; // conv
        if (op >= 0x82 && op <= 0x8D) return 5; // ldstr? no
        if (op >= 0x8E && op <= 0x8F) return 5;
        if (op == 0x90) return 1;
        if (op >= 0x91 && op <= 0xA2) return 1;
        if (op >= 0xA3 && op <= 0xA4) return 5;
        if (op >= 0xA5 && op <= 0xAA) return 5;
        if (op >= 0xAB && op <= 0xAC) return 1;
        if (op >= 0xAD && op <= 0xBD) return 5; // ldfld..stelem
        if (op == 0xBE) return 1; // ldelema? no 5
        if (op >= 0xBF && op <= 0xC5) return 5;
        if (op >= 0xC6 && op <= 0xD1) return 1; // conv/misc, but ldc.i4.m1 etc
        if (op >= 0xD2 && op <= 0xD5) return 5;
        if (op == 0xD6) return 1; // ldtoken? no 5
        if (op == 0xD7) return 5;
        if (op >= 0xD8 && op <= 0xDF) return 1;
        if (op >= 0xE0 && op <= 0xE3) return 1;
        if (op == 0xE4 || op == 0xE5) return 1;
        if (op >= 0xE6 && op <= 0xEB) return 2;
        if (op >= 0xEC && op <= 0xEE) return 5;
        if (op == 0xEF) return 5; // ldtoken
        if (op == 0xF0) return 1;
        if (op >= 0xF1 && op <= 0xF9) return 5;
        if (op >= 0xFA && op <= 0xFE) return 1;
        return 1;
    }

    static string Sanitize(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char c in s) sb.Append(char.IsControl(c) ? '\\' + ((int)c).ToString("X2") : c);
        return sb.ToString();
    }
}
