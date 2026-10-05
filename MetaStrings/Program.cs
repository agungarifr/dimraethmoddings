using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class MetaStrings
{
    static void Main(string[] args)
    {
        string path = args.Length > 0 ? args[0]
            : @"D:\SteamLibrary\steamapps\common\Dimraeth\Dimraeth_Data\il2cpp_data\Metadata\global-metadata.dat";
        var b = File.ReadAllBytes(path);
        var set = new HashSet<string>();
        for (int i = 0; i < b.Length; i++)
        {
            if (b[i] >= 0x20 && b[i] <= 0x7E)
            {
                int j = i;
                while (j < b.Length && b[j] >= 0x20 && b[j] <= 0x7E) j++;
                string s = Encoding.ASCII.GetString(b, i, j - i);
                if (s.Length >= 4 && set.Add(s))
                {
                    if (IsRelevant(s))
                        Console.WriteLine($"0x{i:X8}: {s}");
                }
                i = j - 1;
            }
        }
        Console.WriteLine($"\nTOTAL unique strings: {set.Count}");
    }

    static bool IsRelevant(string s)
    {
        string[] pats = {
            "Heroic","Stoic","Joker","Scholar","Relationship","Opinion","Friendship",
            "Romance","ValueAlign","Archetype","Persona","Dialogue","Greeting","Topic",
            "Option","Tag","GUID","UniqueID","Response","CSV","Col","Row","NPC","Choice",
            "Prompt","Vote","Conversation","Adaptive","BaseOption","InventoryReq","WorldTags",
            "InstanceTags","PlayerTags","relationship"
        };
        return pats.Any(p => s.Contains(p, StringComparison.OrdinalIgnoreCase));
    }
}
