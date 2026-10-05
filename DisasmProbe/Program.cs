using System;
using System.IO;
using System.Linq;
using System.Text;
using Iced.Intel;

string gaPath = @"D:\SteamLibrary\steamapps\common\Dimraeth\GameAssembly.dll";
byte[] bytes = File.ReadAllBytes(gaPath);

int peOffset = BitConverter.ToInt32(bytes, 0x3C);
int numSections = BitConverter.ToInt16(bytes, peOffset + 6);
int optHeaderSize = BitConverter.ToInt16(bytes, peOffset + 20);
int sectionHeadersOffset = peOffset + 24 + optHeaderSize;

var sections = new (uint rva, uint size, uint rawOffset, uint rawSize)[numSections];
for (int i = 0; i < numSections; i++)
{
    int sOff = sectionHeadersOffset + i * 40;
    uint vSize = BitConverter.ToUInt32(bytes, sOff + 8);
    uint vRva = BitConverter.ToUInt32(bytes, sOff + 12);
    uint rawSize = BitConverter.ToUInt32(bytes, sOff + 16);
    uint rawOff = BitConverter.ToUInt32(bytes, sOff + 20);
    sections[i] = (vRva, vSize, rawOff, rawSize);
}

foreach (var s in sections)
    Console.WriteLine($"Section: RVA=0x{s.rva:X8} VSize=0x{s.size:X8} RawOff=0x{s.rawOffset:X8} RawSize=0x{s.rawSize:X8}");

uint RvaToFileOffset(uint rva)
{
    foreach (var s in sections)
        if (rva >= s.rva && rva < s.rva + Math.Min(s.size, s.rawSize))
            return s.rawOffset + (rva - s.rva);
    return 0;
}

void Disasm(string name, uint rva, int length, bool showAll = true)
{
    uint fOff = RvaToFileOffset(rva);
    Console.WriteLine($"\n=== {name} (RVA: 0x{rva:X}, FileOffset: 0x{fOff:X}, Length: 0x{length:X}) CanRead={fOff + (uint)length <= (uint)bytes.Length} ===");
    if (fOff == 0) { Console.WriteLine("  NO MAPPING"); return; }
    var reader = new ByteArrayCodeReader(bytes, (int)fOff, length);
    var decoder = Iced.Intel.Decoder.Create(64, reader);
    decoder.IP = RvaToFileOffset(rva); // print file offsets like the earlier dumps used
    var formatter = new NasmFormatter();
    var output = new StringOutput();
    int count = 0;
    while (reader.CanReadByte && count < 400)
    {
        Iced.Intel.Instruction instr;
        try { decoder.Decode(out instr); } catch { break; }
        output.Reset();
        formatter.Format(instr, output);
        // Print rva + actual machine bytes
        int ipRva = (int)(decoder.IP - RvaToFileOffset(rva));
        Console.WriteLine($"{(rva + (uint)(decoder.IP - RvaToFileOffset(rva))):X8} {output.ToStringAndReset()}");
        count++;
    }
}

string DumpGameConfigFromCpp2il()
{
    string cpp2ilDll = @"D:\SteamLibrary\steamapps\common\Dimraeth\modding\MelonLoader\Dependencies\Il2CppAssemblyGenerator\Cpp2IL\cpp2il_out\Assembly-CSharp.dll";
    if (!File.Exists(cpp2ilDll)) return "cpp2il dll missing";
    var resolver = new ICSharpCode.Decompiler.Metadata.UniversalAssemblyResolver(cpp2ilDll, false, ".NETStandard,Version=v2.1");
    resolver.AddSearchDirectory(Path.GetDirectoryName(cpp2ilDll));
    var dc = new ICSharpCode.Decompiler.CSharp.CSharpDecompiler(cpp2ilDll, resolver, new ICSharpCode.Decompiler.DecompilerSettings());
    var sb = new StringBuilder();
    foreach (var tn in new[] { "GameConfig", "Formulas" })
    {
        try { sb.AppendLine(dc.DecompileTypeAsString(new ICSharpCode.Decompiler.TypeSystem.FullTypeName(tn))); }
        catch (Exception ex) { sb.AppendLine("ERR " + tn + ": " + ex.Message); }
    }
    return sb.ToString();
}

Console.WriteLine("========== GAMECONFIG / FORMULAS (cpp2il) ==========");
Console.WriteLine(DumpGameConfigFromCpp2il());

// The mod's native byte patch targets (as RVAs):
Disasm("XP.IsXPGainBlocked RVA 0x114C2B0", 0x114C2B0, 0x300);
Disasm("PlayerStats.UpgradeAttributeServerRpc RVA 0xA2D8E0", 0xA2D8E0, 0x830);
Disasm("PlayerStats.ApplyUpgradeAttributeInternal RVA 0xA218B0", 0xA218B0, 0x5F0);
Disasm("PlayerStats.XPRequiredForLevelUp RVA 0xA2E700", 0xA2E700, 0x130);
Disasm("Player.InitializeHighestLevel RVA 0x92AFC0", 0x92AFC0, 0x400);
Disasm("Player.ValidateLoadedXPData RVA 0x933040", 0x933040, 0x400);
Disasm("XP.AddXPToPlayer RVA 0x114AD20", 0x114AD20, 0x2A0);
Disasm("XP.GrantXPToPlayer RVA 0x114BE40", 0x114BE40, 0x280);
Disasm("XP.GrantXP RVA 0x114C0C0", 0x114C0C0, 0x1F0);
Disasm("PlayerStats.UpgradeLevelClientRpc RVA 0xA2E110", 0xA2E110, 0x200);
Disasm("Player.levelup-trigger 0x9302E0", 0x9302E0, 0x400);
Disasm("PS.UpgradeLevelClientRpc site 0xA2E3E0", 0xA2E3E0, 0x90);
Disasm("PS.Update site 0xA2CFD0", 0xA2CFD0, 0x80);
Disasm("PlayerUpgradeUI.CanReachNextLevel 0x9BE410", 0x9BE410, 0x450);
Disasm("PlayerUpgradeUI.UpdateUpgradeButtonInteractables 0x9C2FB0", 0x9C2FB0, 0x600);
Disasm("PlayerUpgradeUI.RecomputeSimulation 0x9C0400", 0x9C0400, 0xA30);
Disasm("PUUI.ConfirmUpgradePurchase 0x9BEBA0", 0x9BEBA0, 0x200);
Disasm("PUUI.Update 0x9C35B0", 0x9C35B0, 0x610);
Disasm("PUUI.WireButtonListeners 0x9C3BC0", 0x9C3BC0, 0x620);
Disasm("PUUI.sub_BE160 (Update caller)", 0x9BE160, 0x2B0);
Disasm("PUUI.sub_9C0D30 (Update caller)", 0x9C0D30, 0x100);
Disasm("PUUI.sub_9BEF60 (Update caller)", 0x9BEF60, 0x100);
Disasm("PUUI.sub_9BFE50 (Update caller)", 0x9BFE50, 0x100);