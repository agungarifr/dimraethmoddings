using System;
using System.IO;

string gaPath = @"D:\SteamLibrary\steamapps\common\Dimraeth\GameAssembly.dll";
byte[] b = File.ReadAllBytes(gaPath);

int peOffset = BitConverter.ToInt32(b, 0x3C);
int numSections = BitConverter.ToInt16(b, peOffset + 6);
int optHeaderSize = BitConverter.ToInt16(b, peOffset + 20);
int sectionHeadersOffset = peOffset + 24 + optHeaderSize;
var sections = new (uint rva, uint size, uint rawOffset, uint rawSize)[numSections];
for (int i = 0; i < numSections; i++)
{
    int sOff = sectionHeadersOffset + i * 40;
    sections[i] = (BitConverter.ToUInt32(b, sOff + 12), BitConverter.ToUInt32(b, sOff + 8),
                   BitConverter.ToUInt32(b, sOff + 20), BitConverter.ToUInt32(b, sOff + 16));
}
uint RvaToFileOffset(uint rva)
{
    foreach (var s in sections)
        if (rva >= s.rva && rva < s.rva + Math.Min(s.size, s.rawSize))
            return s.rawOffset + (rva - s.rva);
    return 0;
}

void DumpBytes(string label, uint rva, int len)
{
    uint fOff = RvaToFileOffset(rva);
    Console.WriteLine($"--- {label} RVA=0x{rva:X} File=0x{fOff:X}");
    if (fOff == 0) { Console.WriteLine("  no mapping"); return; }
    for (int i = 0; i < len; i += 16)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"  {rva + (uint)i:X8}: ");
        for (int j = 0; j < 16 && i + j < len; j++) sb.Append(b[fOff + i + j].ToString("X2") + " ");
        Console.WriteLine(sb.ToString());
    }
}

// XP.IsXPGainBlocked compare sites (from probe: cmp at 0x114C32E, 0x114C364, mov at 0x114C36D)
DumpBytes("XP.IsXPGainBlocked", 0x114C320, 0x60);
// Mod's claimed XP sites
DumpBytes("mod target 0x114C32B", 0x114C32B, 8);
DumpBytes("mod target 0x114C361", 0x114C361, 8);
DumpBytes("mod target 0x114C368", 0x114C368, 8);
// InitializeHighestLevel site
DumpBytes("initHighest 0x92C0A0", 0x92C0A0, 0x90);
// ApplyUpgradeAttributeInternal
DumpBytes("applyAttr 0xA21960", 0xA21960, 0x10);
DumpBytes("applyAttr 0xA21D30", 0xA21D30, 0x10);
// UpgradeAttributeServerRpc
DumpBytes("upgAttr 0xA2DB50", 0xA2DB50, 0x10);
DumpBytes("upgAttr 0xA2DED0", 0xA2DED0, 0x10);
DumpBytes("attrCap 0xA219D4", 0xA219D4, 0x10);
DumpBytes("attrCap 0xA2DBC8", 0xA2DBC8, 0x10);
DumpBytes("UI cap CanReach 0x9BE527", 0x9BE527, 0x10);
DumpBytes("UI cap SimLevel 0x9C3158", 0x9C3158, 0x18);
DumpBytes("UI cap Recompute1 0x9C066B", 0x9C066B, 0x20);
DumpBytes("UI cap Recompute2 0x9C0750", 0x9C0750, 0x18);
DumpBytes("UI cap Recompute3 0x9C0B50", 0x9C0B50, 0x18);
DumpBytes("UI cap TrySpend 0x9C2574", 0x9C2574, 0x18);
DumpBytes("UI cap Reset 0x9C1614", 0x9C1614, 0x18);
