// [2026-09-28 20:50] RESTORED: LevelCapModule ("Configurable Level Cap").
// Context: this module existed in DimraethModPack on/before 2026-09-25 (registered in
// DimraethModPackPlugin, config section [System.LevelCap]) but disappeared from the
// source tree AND from the compiled DimraethModPack.dll without any "Obsolete" note --
// most likely lost during the 2026-09-25 backup/restore cycle for the elf_error_move_random_crash
// fix. The live config BepInEx/config/com.custom.dimraethmodpack.cfg still carried the stale
// [System.LevelCap] section (Enabled=true, MaxLevelCap=45, MaxAttributeCap=75), which silently
// did nothing while the module was missing.
// Restored verbatim from
//   modding/Backup_2026-09-25_09-15_fix_elf_move_crash_untested/BepInExModsSource/DimraethModPack/Modules/System/LevelCapModule.cs
// The standalone ConfigurableLevelCapFreebuff (customlevelcap.dll) was NOT used as a reference for
// the restore itself, per user instruction at the time. Config key names match the stale cfg section
// exactly, so existing values (45/75) bind unchanged.
// [2026-09-29 09:28] UPDATE: the restored byte patcher (64-byte scan window, early-ret stop, no logging)
// silently patched NOTHING -> level cap stuck at vanilla 25. Per the user's new instruction the
// 2026-09-22 deploy of customlevelcap.dll (v1.4.0) was decompiled
// (modding/DecompilerTool/clcap_out/) and its proven scanner ported into NativeBytePatcher below.
// The standalone DLL itself is left untouched; the user removes it manually. Do NOT run both plugins
// at once -- they patch the same native sites with different values.
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace DimraethModPack.Modules.SystemMod
{
    public class LevelCapModule : ModModuleBase
    {
        public static LevelCapModule Instance { get; private set; }

        public override string Category => "System";
        public override string Name => "Configurable Level Cap";
        public override string Description => "Raises maximum character level and attribute caps up to level 99";

        public ConfigEntry<int> MaxLevelCap;
        public ConfigEntry<int> MaxAttributeCap;
        public ConfigEntry<bool> EnableNativeBytePatch;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "System.LevelCap";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Configurable Level Cap Module (Default: false, Vanilla: false)");

            MaxLevelCap = config.Bind(sec, "MaxLevelCap", 99,
                "Maximum character level ceiling (Default: 99, Vanilla: 25)");

            MaxAttributeCap = config.Bind(sec, "MaxAttributeCap", 99,
                "Maximum attribute point allocation ceiling (Default: 99, Vanilla: 25)");

            EnableNativeBytePatch = config.Bind(sec, "EnableNativeBytePatch", false,
                "Patch native cmp 0x19 instructions in runtime memory (Default: false, Vanilla: false)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
            // [2026-09-29 08:51] New: startup status log so the restored module is verifiable in LogOutput.log
            // (previously it only appeared via the generic "config bound / patches applied" lines).
            DimraethModPackPlugin.Log?.LogInfo(
                $"[LevelCap] Status: Enabled={IsEnabled}, MaxLevelCap={MaxLevelCap?.Value}, MaxAttributeCap={MaxAttributeCap?.Value}, NativeBytePatch={(EnableNativeBytePatch != null && EnableNativeBytePatch.Value ? "ON" : "off")}");
            if (IsEnabled && EnableNativeBytePatch.Value)
            {
                ApplyNativePatch();
            }
        }

        public void ApplyNativePatch()
        {
            try
            {
                NativeBytePatcher.Apply(MaxLevelCap.Value, MaxAttributeCap.Value);
            }
            // [2026-09-29 09:28] Old silent catch kept per repo rule (commented below). Failures must be
            // visible -- invisible failures are exactly why the "level stuck at 25" bug went unnoticed.
            // catch { }
            catch (Exception ex)
            {
                DimraethModPackPlugin.Log?.LogError($"[LevelCap] Native byte patch failed: {ex}");
            }
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawIntDualSpinner(x, curY, width, "Max Level Cap", MaxLevelCap, 1, 99, "Vanilla: 25", labelStyle, btnStyle, val =>
            {
                if (EnableNativeBytePatch.Value) ApplyNativePatch();
            });

            curY += DrawIntDualSpinner(x, curY, width, "Max Attribute Cap", MaxAttributeCap, 1, 99, "Vanilla: 25", labelStyle, btnStyle, val =>
            {
                if (EnableNativeBytePatch.Value) ApplyNativePatch();
            });

            curY += DrawToggle(x, curY, width, "Native Byte Patch", EnableNativeBytePatch, "Default: ON", labelStyle, btnStyle, val =>
            {
                if (val) ApplyNativePatch();
            });
            return curY - y;
        }

        public static class Patches
        {
            [HarmonyPatch(typeof(XP), nameof(XP.IsXPGainBlocked))]
            [HarmonyPrefix]
            public static bool Prefix_IsXPGainBlocked(Player player, ref bool __result)
            {
                if (Instance == null || !Instance.IsEnabled) return true;
                if (player == null || player.Level == null) return true;

                int level = player.Level.Value;
                int cap = Instance.MaxLevelCap.Value;
                __result = level >= cap;
                return false;
            }
        }

        public static class NativeBytePatcher
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

            private sealed class MethodTarget
            {
                public Type Type;
                public string MethodName;
                public int ParamCount;
                public byte VanillaImm;
                public string Name;
            }

            private static readonly MethodTarget[] Targets = new[]
            {
                new MethodTarget { Type = typeof(XP),              MethodName = "IsXPGainBlocked",               ParamCount = 1, VanillaImm = 0x19, Name = "XP.IsXPGainBlocked" },
                new MethodTarget { Type = typeof(Player),          MethodName = "InitializeHighestLevel",        ParamCount = 0, VanillaImm = 0x19, Name = "Player.InitializeHighestLevel" },
                // [2026-09-29 09:28] Renamed with "(level)" suffix; 2 "(attr)" targets appended at the end of this
                // list. Old lines kept per repo rule:
                // new MethodTarget { Type = typeof(PlayerStats),     MethodName = "ApplyUpgradeAttributeInternal",  ParamCount = 2, VanillaImm = 0x19, Name = "PlayerStats.ApplyUpgradeAttributeInternal" },
                // new MethodTarget { Type = typeof(PlayerStats),     MethodName = "UpgradeAttributeServerRpc",      ParamCount = 2, VanillaImm = 0x19, Name = "PlayerStats.UpgradeAttributeServerRpc" },
                new MethodTarget { Type = typeof(PlayerStats),     MethodName = "ApplyUpgradeAttributeInternal",  ParamCount = 2, VanillaImm = 0x19, Name = "PlayerStats.ApplyUpgradeAttributeInternal (level)" },
                new MethodTarget { Type = typeof(PlayerStats),     MethodName = "UpgradeAttributeServerRpc",      ParamCount = 2, VanillaImm = 0x19, Name = "PlayerStats.UpgradeAttributeServerRpc (level)" },
                new MethodTarget { Type = typeof(PlayerUpgradeUI), MethodName = "CanReachNextLevel",              ParamCount = 0, VanillaImm = 0x19, Name = "PlayerUpgradeUI.CanReachNextLevel" },
                new MethodTarget { Type = typeof(PlayerUpgradeUI), MethodName = "UpdateUpgradeButtonInteractables", ParamCount = 0, VanillaImm = 0x19, Name = "PlayerUpgradeUI.UpdateUpgradeButtonInteractables" },
                new MethodTarget { Type = typeof(PlayerUpgradeUI), MethodName = "RecomputeSimulation",            ParamCount = 0, VanillaImm = 0x19, Name = "PlayerUpgradeUI.RecomputeSimulation" },
                new MethodTarget { Type = typeof(PlayerUpgradeUI), MethodName = "TrySpendPoint",                  ParamCount = 0, VanillaImm = 0x19, Name = "PlayerUpgradeUI.TrySpendPoint" },
                new MethodTarget { Type = typeof(PlayerUpgradeUI), MethodName = "ResetAllValues",                 ParamCount = 0, VanillaImm = 0x19, Name = "PlayerUpgradeUI.ResetAllValues" },
                // [2026-09-29 09:28] New (from working standalone customlevelcap.dll v1.4.0): the per-attribute
                // hard-cap sites compare against GameConfig.MaxAttributeLevel = 99 (imm 0x63), NOT 0x19, and are
                // replaced with MaxAttributeCap. The broken version only had the 0x19 sites above and ignored
                // attributeCap entirely.
                new MethodTarget { Type = typeof(PlayerStats),     MethodName = "ApplyUpgradeAttributeInternal",  ParamCount = 2, VanillaImm = 0x63, Name = "PlayerStats.ApplyUpgradeAttributeInternal (attr)" },
                new MethodTarget { Type = typeof(PlayerStats),     MethodName = "UpgradeAttributeServerRpc",      ParamCount = 2, VanillaImm = 0x63, Name = "PlayerStats.UpgradeAttributeServerRpc (attr)" },
            };

            // [2026-09-29 09:28] Changed HashSet<IntPtr> -> Dictionary<IntPtr, byte> (addr -> vanilla imm it
            // replaced). The F8 UI calls Apply() on every cap change and sites patched earlier no longer match
            // the vanilla-imm scan, so we must remember where they are (and whether they are level 0x19 or
            // attr 0x63 sites) to rewrite them live. Old line kept per repo rule:
            // private static readonly System.Collections.Generic.HashSet<IntPtr> _patchedAddresses = new();
            private static readonly System.Collections.Generic.Dictionary<IntPtr, byte> _patchedAddresses = new();

            // [2026-09-29 09:28] New (from standalone v1.4.0): remember last applied caps so Apply() is idempotent.
            private static int _appliedLevel;
            private static int _appliedAttr;

            private static IntPtr GetClassPtr(Type type)
            {
                var storeType = typeof(Il2CppClassPointerStore<>).MakeGenericType(type);
                RuntimeHelpers.RunClassConstructor(storeType.TypeHandle);
                var field = storeType.GetField("NativeClassPtr", BindingFlags.Public | BindingFlags.Static);
                return field != null ? (IntPtr)field.GetValue(null) : IntPtr.Zero;
            }

            // [2026-09-29 09:28] Old Apply() kept per repo rule (commented out below). Why it was broken:
            //   1) the attributeCap parameter was ignored (only levelCap was ever written);
            //   2) ScanMethodForCmpImm8 saw just 64 bytes and stopped at the first `ret`, so the cap cmp
            //      sites (usually past byte 64 / past early returns) were never found -> 0 sites patched;
            //   3) all failures were swallowed by `catch { }` / `continue`, so nothing appeared in the log.
            // Replaced with the proven standalone customlevelcap.dll v1.4.0 logic (decompiled 2026-09-29 from
            // the 2026-09-22 deploy via modding/DecompilerTool/clcap_out/), plus live cap re-apply (the
            // standalone read caps once at startup only; our F8 UI changes caps at runtime).
            // public static void Apply(int levelCap, int attributeCap)
            // {
            //     byte targetLevel = (byte)Math.Clamp(levelCap, 1, 99);
            //     foreach (var target in Targets)
            //     {
            //         try
            //         {
            //             IntPtr classPtr = GetClassPtr(target.Type);
            //             if (classPtr == IntPtr.Zero) continue;
            //
            //             IntPtr methodInfo = IntPtr.Zero;
            //             for (int count = 0; count <= 6; count++)
            //             {
            //                 methodInfo = IL2CPP.il2cpp_class_get_method_from_name(classPtr, target.MethodName, count);
            //                 if (methodInfo != IntPtr.Zero) break;
            //             }
            //
            //             if (methodInfo == IntPtr.Zero) continue;
            //
            //             IntPtr codePtr = Marshal.ReadIntPtr(methodInfo);
            //             if (codePtr == IntPtr.Zero) continue;
            //
            //             ScanMethodForCmpImm8(codePtr, target.VanillaImm, targetLevel);
            //         }
            //         catch { }
            //     }
            // }

            public static void Apply(int levelCap, int attributeCap)
            {
                byte targetLevel = (byte)Math.Clamp(levelCap, 1, 99);
                byte targetAttr = (byte)Math.Clamp(attributeCap, 1, 99);
                if (_appliedLevel == targetLevel && _appliedAttr == targetAttr && _patchedAddresses.Count > 0) return;

                // Rewrite sites patched earlier so cap changes in the F8 UI apply without a game restart.
                int updated = 0;
                foreach (var kv in _patchedAddresses)
                {
                    byte desired = kv.Value == 0x63 ? targetAttr : targetLevel;
                    if (Marshal.ReadByte(kv.Key) == desired) continue;
                    if (!VirtualProtect(kv.Key, (UIntPtr)1, 0x40, out uint wpOld)) continue;
                    Marshal.WriteByte(kv.Key, desired);
                    VirtualProtect(kv.Key, (UIntPtr)1, wpOld, out _);
                    updated++;
                }

                int sitesPatched = 0, methodsAtCap = 0, methodsUnresolved = 0;
                foreach (var target in Targets)
                {
                    byte replacement = target.VanillaImm == 0x63 ? targetAttr : targetLevel;
                    int patched = 0, atCap = 0;
                    try
                    {
                        IntPtr classPtr = GetClassPtr(target.Type);
                        if (classPtr == IntPtr.Zero)
                        {
                            DimraethModPackPlugin.Log?.LogWarning($"[LevelCap] No IL2CPP class pointer for {target.Type.Name}");
                            methodsUnresolved++;
                            continue;
                        }

                        IntPtr methodInfo = IntPtr.Zero;
                        int foundParams = -1;
                        for (int count = 0; count <= 6; count++)
                        {
                            methodInfo = IL2CPP.il2cpp_class_get_method_from_name(classPtr, target.MethodName, count);
                            if (methodInfo != IntPtr.Zero) { foundParams = count; break; }
                        }

                        if (methodInfo == IntPtr.Zero)
                        {
                            DimraethModPackPlugin.Log?.LogWarning($"[LevelCap] Method not found: {target.Name} (tried paramCount={target.ParamCount})");
                            methodsUnresolved++;
                            continue;
                        }

                        if (foundParams != target.ParamCount)
                        {
                            DimraethModPackPlugin.Log?.LogInfo($"[LevelCap] {target.Name}: found with paramCount={foundParams} (expected {target.ParamCount})");
                        }

                        IntPtr codePtr = Marshal.ReadIntPtr(methodInfo);
                        if (codePtr == IntPtr.Zero)
                        {
                            DimraethModPackPlugin.Log?.LogWarning($"[LevelCap] Null code pointer for {target.Name}");
                            methodsUnresolved++;
                            continue;
                        }

                        DimraethModPackPlugin.Log?.LogInfo($"[LevelCap] {target.Name}: native code @ 0x{codePtr.ToInt64():X}");
                        (patched, atCap) = ScanMethodForCmpImm8(codePtr, target.VanillaImm, replacement, target.Name);
                    }
                    catch (Exception ex)
                    {
                        DimraethModPackPlugin.Log?.LogError($"[LevelCap] Error scanning {target.Name}: {ex}");
                        methodsUnresolved++;
                        continue;
                    }

                    sitesPatched += patched;
                    if (patched == 0 && atCap > 0) methodsAtCap++;
                    else if (patched == 0 && atCap == 0) methodsUnresolved++;
                }

                _appliedLevel = targetLevel;
                _appliedAttr = targetAttr;
                DimraethModPackPlugin.Log?.LogInfo(
                    $"[LevelCap] Byte patch result: {sitesPatched} sites patched, {updated} updated, {methodsAtCap} methods already at cap, {methodsUnresolved} methods unresolved (level cap={targetLevel}, attribute cap={targetAttr})");
            }

            // [2026-09-29 09:28] Old scanner kept per repo rule (commented out below). The 64-byte window plus
            // the early `ret` stop found nothing -- the root cause of "level stuck at 25" (failures were
            // silent). Replaced with the standalone v1.4.0 scanner: 8192-byte window, no early stop, per-site
            // log with hex context, and "already at cap" accounting for sites claimed by overlapping scans.
            // private static void ScanMethodForCmpImm8(IntPtr codePtr, byte vanilla, byte replacement)
            // {
            //     const int maxScan = 64;
            //     byte[] buf = new byte[maxScan];
            //     Marshal.Copy(codePtr, buf, 0, maxScan);
            //
            //     for (int i = 2; i < maxScan - 1; i++)
            //     {
            //         if (buf[i] == 0xC3 && i > 8) break; // Stop at ret instruction
            //         if (buf[i] != vanilla) continue;
            //         if (!IsCmpImm8(buf, i)) continue;
            //
            //         IntPtr addr = IntPtr.Add(codePtr, i);
            //         if (VirtualProtect(addr, (UIntPtr)1, 0x40, out uint oldProtect))
            //         {
            //             Marshal.WriteByte(addr, replacement);
            //             VirtualProtect(addr, (UIntPtr)1, oldProtect, out _);
            //             _patchedAddresses.Add(addr);
            //         }
            //     }
            // }

            private static (int Patched, int AlreadyAtCap) ScanMethodForCmpImm8(IntPtr codePtr, byte vanilla, byte replacement, string name)
            {
                const int maxScan = 8192;
                byte[] buf = new byte[maxScan];
                Marshal.Copy(codePtr, buf, 0, maxScan);

                int patched = 0;
                for (int i = 2; i < maxScan - 1; i++)
                {
                    if (buf[i] != vanilla || !IsCmpImm8(buf, i)) continue;

                    IntPtr addr = IntPtr.Add(codePtr, i);
                    if (!VirtualProtect(addr, (UIntPtr)1, 0x40, out uint oldProtect)) continue;
                    Marshal.WriteByte(addr, replacement);
                    VirtualProtect(addr, (UIntPtr)1, oldProtect, out _);
                    if (!_patchedAddresses.ContainsKey(addr)) _patchedAddresses.Add(addr, vanilla);
                    patched++;
                    DimraethModPackPlugin.Log?.LogInfo(
                        $"[LevelCap] Patched {name}: 0x{vanilla:X2}->0x{replacement:X2} @ method+0x{i:X}  {GetHexContext(buf, i)}");
                }

                // NOTE: buf is the pre-patch copy, so this counts sites that were already at the replacement
                // value when scanned (patched by a previous Apply() call or claimed by an overlapping scan).
                int alreadyAtCap = 0;
                for (int j = 2; j < maxScan - 1; j++)
                {
                    if (buf[j] == replacement && IsCmpImm8(buf, j) && _patchedAddresses.ContainsKey(IntPtr.Add(codePtr, j)))
                        alreadyAtCap++;
                }

                if (patched == 0 && alreadyAtCap == 0)
                    DimraethModPackPlugin.Log?.LogWarning($"[LevelCap] No cmp imm8=0x{vanilla:X2} found in {name}");
                else if (patched == 0 && alreadyAtCap > 0)
                    DimraethModPackPlugin.Log?.LogInfo($"[LevelCap] {name}: {alreadyAtCap} cmp site(s) already at 0x{replacement:X2} (claimed by overlapping scan)");

                return (patched, alreadyAtCap);
            }

            // [2026-09-29 09:28] New (from standalone v1.4.0): 9-byte hex context around a patched site for the log.
            private static string GetHexContext(byte[] buf, int pos)
            {
                int start = Math.Max(0, pos - 4);
                int end = Math.Min(buf.Length - 1, pos + 4);
                var sb = new System.Text.StringBuilder("[");
                for (int i = start; i <= end; i++)
                {
                    if (i == pos) sb.Append(">>>");
                    sb.Append("0x").Append(buf[i].ToString("X2"));
                    if (i == pos) sb.Append("<<<");
                    if (i < end) sb.Append(' ');
                }
                return sb.Append(']').ToString();
            }

            private static bool IsCmpImm8(byte[] buf, int pos)
            {
                for (int opcodePos = pos - 8; opcodePos <= pos - 2; opcodePos++)
                {
                    if (opcodePos < 0) continue;
                    int p = opcodePos;
                    if ((buf[p] & 0xF0) == 0x40) { p++; if (p >= buf.Length) continue; }
                    if (buf[p] != 0x83) continue;
                    p++; if (p >= buf.Length) continue;
                    byte modrm = buf[p];
                    if ((modrm & 0x38) != 0x38) continue;
                    p++; if (p >= buf.Length) continue;
                    int mod = (modrm >> 6) & 3;
                    int rm = modrm & 7;
                    if (mod != 3 && rm == 4) { p++; if (p >= buf.Length) continue; }
                    if (mod == 0 && rm == 5) p += 4;
                    else if (mod == 1) p += 1;
                    else if (mod == 2) p += 4;
                    if (p == pos) return true;
                }
                return false;
            }
        }
    }
}
