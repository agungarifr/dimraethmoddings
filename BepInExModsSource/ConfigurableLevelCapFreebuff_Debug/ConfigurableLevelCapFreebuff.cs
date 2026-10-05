using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;

namespace ConfigurableLevelCapFreebuff
{
    [BepInPlugin("com.freebuff.configurablelevelcap", "ConfigurableLevelCapFreebuff", "1.4.0")]
    public class ConfigurableLevelCapPlugin : BasePlugin
    {
        public static ConfigurableLevelCapPlugin Instance { get; private set; }
        internal static new ManualLogSource Log;

        public static ConfigEntry<int> MaxLevelCap;
        public static ConfigEntry<int> MaxAttributeCap;
        public static ConfigEntry<float> ExpMultiplier;
        public static ConfigEntry<bool> EnableNativeBytePatch;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            MaxLevelCap = Config.Bind("Progression", "MaxLevelCap", 99,
                "Maximum character level. Set to 25 for vanilla. Engine supports up to 99.");
            MaxAttributeCap = Config.Bind("Progression", "MaxAttributeCap", 99,
                "Maximum per-attribute level. Engine supports up to 99.");
            ExpMultiplier = Config.Bind("Progression", "ExpMultiplier", 1.0f,
                "EXP multiplier for all sources. 1.0 = vanilla.");
            EnableNativeBytePatch = Config.Bind("General", "EnableNativeBytePatch", true,
                "Patch native GameAssembly.dll byte code (pattern-scanned, version-independent). Safe to disable.");

            var harmony = new Harmony("com.freebuff.configurablelevelcap");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo("=================================================");
            Log.LogInfo("ConfigurableLevelCapFreebuff v1.4.0 Initialized!");
            Log.LogInfo($"Max Level Cap: {MaxLevelCap.Value}");
            Log.LogInfo($"Max Attribute Cap: {MaxAttributeCap.Value}");
            Log.LogInfo($"EXP Multiplier: {ExpMultiplier.Value}x");
            Log.LogInfo($"Native Byte Patch: {(EnableNativeBytePatch.Value ? "ENABLED" : "disabled")}");
            Log.LogInfo("Uses IL2CPP runtime method resolution + pattern scanning.");
            Log.LogInfo("=================================================");

            if (EnableNativeBytePatch.Value)
                NativeBytePatcher.Apply(MaxLevelCap.Value, MaxAttributeCap.Value);
        }
    }

    // ============================================================================
    // NATIVE BYTE PATCH v2: Pattern-scanning, version-independent.
    //
    // Instead of hardcoded RVAs (which break on every game update), we:
    //   1. Resolve each method's native code address via IL2CPP at runtime
    //   2. Scan the method body for 'cmp r/m, imm8' instructions
    //   3. Patch the immediate byte (0x19→levelCap, 0x63→attributeCap)
    //
    // This works across game versions because method names are stable and
    // the IL2CPP runtime resolves the correct address for the current build.
    // ============================================================================
    public static class NativeBytePatcher
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize,
            uint flNewProtect, out uint lpflOldProtect);

        private sealed class MethodTarget
        {
            public Type Type;
            public string MethodName;
            public int ParamCount;
            public byte VanillaImm;
            public string Name;
        }

        // Methods containing inlined level-cap (25=0x19) or attribute-cap (99=0x63) checks.
        // ParamCount = declared parameters excluding 'this' for instance methods.
        private static readonly MethodTarget[] Targets = new[]
        {
            // ---- Level cap (0x19 = 25) ----
            new MethodTarget { Type = typeof(XP),              MethodName = "IsXPGainBlocked",               ParamCount = 1, VanillaImm = 0x19, Name = "XP.IsXPGainBlocked" },
            new MethodTarget { Type = typeof(Player),          MethodName = "InitializeHighestLevel",        ParamCount = 0, VanillaImm = 0x19, Name = "Player.InitializeHighestLevel" },
            new MethodTarget { Type = typeof(PlayerStats),     MethodName = "ApplyUpgradeAttributeInternal",  ParamCount = 2, VanillaImm = 0x19, Name = "PlayerStats.ApplyUpgradeAttributeInternal (level)" },
            new MethodTarget { Type = typeof(PlayerStats),     MethodName = "UpgradeAttributeServerRpc",      ParamCount = 2, VanillaImm = 0x19, Name = "PlayerStats.UpgradeAttributeServerRpc (level)" },
            new MethodTarget { Type = typeof(PlayerUpgradeUI), MethodName = "CanReachNextLevel",              ParamCount = 0, VanillaImm = 0x19, Name = "PlayerUpgradeUI.CanReachNextLevel" },
            new MethodTarget { Type = typeof(PlayerUpgradeUI), MethodName = "UpdateUpgradeButtonInteractables", ParamCount = 0, VanillaImm = 0x19, Name = "PlayerUpgradeUI.UpdateUpgradeButtonInteractables" },
            new MethodTarget { Type = typeof(PlayerUpgradeUI), MethodName = "RecomputeSimulation",            ParamCount = 0, VanillaImm = 0x19, Name = "PlayerUpgradeUI.RecomputeSimulation" },
            new MethodTarget { Type = typeof(PlayerUpgradeUI), MethodName = "TrySpendPoint",                  ParamCount = 0, VanillaImm = 0x19, Name = "PlayerUpgradeUI.TrySpendPoint" },
            new MethodTarget { Type = typeof(PlayerUpgradeUI), MethodName = "ResetAllValues",                 ParamCount = 0, VanillaImm = 0x19, Name = "PlayerUpgradeUI.ResetAllValues" },
            // ---- Attribute cap (0x63 = 99) ----
            new MethodTarget { Type = typeof(PlayerStats),     MethodName = "ApplyUpgradeAttributeInternal",  ParamCount = 2, VanillaImm = 0x63, Name = "PlayerStats.ApplyUpgradeAttributeInternal (attr)" },
            new MethodTarget { Type = typeof(PlayerStats),     MethodName = "UpgradeAttributeServerRpc",      ParamCount = 2, VanillaImm = 0x63, Name = "PlayerStats.UpgradeAttributeServerRpc (attr)" },
        };

        private static int _appliedLevel;
        private static int _appliedAttr;

        public static void Apply(int levelCap, int attributeCap)
        {
            int levelByte = Math.Clamp(levelCap, 1, 99);
            int attrByte  = Math.Clamp(attributeCap, 1, 99);

            if (_appliedLevel == levelByte && _appliedAttr == attrByte) return;

            int totalPatched    = 0;
            int totalAlreadyCap = 0;
            int totalUnresolved = 0;

            foreach (var target in Targets)
            {
                byte replacement = (byte)(target.VanillaImm == 0x63 ? attrByte : levelByte);
                var (patched, alreadyAtCap) = ScanAndPatch(target, replacement);
                if (patched > 0) totalPatched += patched;
                else if (alreadyAtCap > 0) totalAlreadyCap++;
                else totalUnresolved++;
            }

            _appliedLevel = levelByte;
            _appliedAttr  = attrByte;
            ConfigurableLevelCapPlugin.Log?.LogInfo(
                $"[ConfigurableLC] Pattern-scan result: {totalPatched} sites patched, " +
                $"{totalAlreadyCap} methods already at cap, {totalUnresolved} methods unresolved " +
                $"(level cap={levelByte}, attribute cap={attrByte})");
        }

        /// <summary>
        /// Gets the IL2CPP native class pointer for a C# proxy type.
        /// </summary>
        private static IntPtr GetClassPtr(Type type)
        {
            var storeType = typeof(Il2CppClassPointerStore<>).MakeGenericType(type);
            RuntimeHelpers.RunClassConstructor(storeType.TypeHandle);
            var field = storeType.GetField("NativeClassPtr", BindingFlags.Public | BindingFlags.Static);
            return field != null ? (IntPtr)field.GetValue(null) : IntPtr.Zero;
        }

        // Physical addresses already patched during the current Apply() run.
        // Used to attribute overlapping targets correctly: RecomputeSimulation+0x1220 is the
        // same byte as ResetAllValues+0x4D0, and TrySpendPoint+0x42 is the same byte as
        // ResetAllValues+0x1432 (scan windows of adjacent methods overlap in memory).
        private static readonly System.Collections.Generic.HashSet<IntPtr> _patchedAddresses =
            new System.Collections.Generic.HashSet<IntPtr>();

        /// <summary>
        /// Resolves a method via IL2CPP, scans its native code for cmp imm8 patterns,
        /// and patches the immediate byte.
        /// </summary>
        private static (int Patched, int AlreadyAtCap) ScanAndPatch(MethodTarget target, byte replacement)
        {
            try
            {
                IntPtr classPtr = GetClassPtr(target.Type);
                if (classPtr == IntPtr.Zero)
                {
                    ConfigurableLevelCapPlugin.Log?.LogWarning(
                        $"[ConfigurableLC] No IL2CPP class pointer for {target.Type.Name}");
                    return (0, 0);
                }

                // Try specified paramCount first, then broader range as fallback
                IntPtr methodInfo = IntPtr.Zero;
                int foundParamCount = -1;
                for (int count = 0; count <= 6; count++)
                {
                    methodInfo = IL2CPP.il2cpp_class_get_method_from_name(classPtr, target.MethodName, count);
                    if (methodInfo != IntPtr.Zero)
                    {
                        foundParamCount = count;
                        break;
                    }
                }

                if (methodInfo == IntPtr.Zero)
                {
                    ConfigurableLevelCapPlugin.Log?.LogWarning(
                        $"[ConfigurableLC] Method not found: {target.Name} " +
                        $"(tried paramCount={target.ParamCount})");
                    return (0, 0);
                }

                if (foundParamCount != target.ParamCount)
                {
                    ConfigurableLevelCapPlugin.Log?.LogInfo(
                        $"[ConfigurableLC] {target.Name}: found with paramCount={foundParamCount} " +
                        $"(expected {target.ParamCount})");
                }

                // Read the native code pointer from MethodInfo (offset 0 = methodPointer)
                IntPtr codePtr = Marshal.ReadIntPtr(methodInfo);
                if (codePtr == IntPtr.Zero)
                {
                    ConfigurableLevelCapPlugin.Log?.LogWarning(
                        $"[ConfigurableLC] Null code pointer for {target.Name}");
                    return (0, 0);
                }

                ConfigurableLevelCapPlugin.Log?.LogInfo(
                    $"[ConfigurableLC] {target.Name}: native code @ 0x{codePtr.ToInt64():X}");

                // Scan the method body for cmp r/m, imm8 with the target immediate
                return ScanMethodForCmpImm8(codePtr, target.VanillaImm, replacement, target.Name);
            }
            catch (Exception ex)
            {
                ConfigurableLevelCapPlugin.Log?.LogError(
                    $"[ConfigurableLC] Error scanning {target.Name}: {ex}");
                return (0, 0);
            }
        }

        /// <summary>
        /// Scans a native method's code for 'cmp r/m, imm8' instructions where the
        /// immediate byte matches 'vanilla', and replaces it with 'replacement'.
        ///
        /// Overlapping windows: adjacent methods' 8192-byte windows overlap in memory, so a
        /// site may first be claimed (patched) by an earlier target. For such targets we count
        /// the site as "already at cap" instead of unresolved. Only addresses actually patched
        /// during this Apply() run are credited, so unrelated cmp-0x32 instructions are ignored.
        /// </summary>
        private static (int Patched, int AlreadyAtCap) ScanMethodForCmpImm8(IntPtr codePtr, byte vanilla, byte replacement, string name)
        {
            const int maxScan = 8192;
            byte[] buf = new byte[maxScan];
            Marshal.Copy(codePtr, buf, 0, maxScan);

            int patched = 0;
            for (int i = 2; i < maxScan - 1; i++)
            {
                if (buf[i] != vanilla) continue;
                if (!IsCmpImm8(buf, i)) continue;

                IntPtr addr = IntPtr.Add(codePtr, i);
                if (VirtualProtect(addr, (UIntPtr)1, 0x40 /* PAGE_EXECUTE_READWRITE */, out uint oldProtect))
                {
                    Marshal.WriteByte(addr, replacement);
                    VirtualProtect(addr, (UIntPtr)1, oldProtect, out _);
                    patched++;
                    _patchedAddresses.Add(addr);

                    // Log surrounding bytes for verification
                    string context = GetHexContext(buf, i);
                    ConfigurableLevelCapPlugin.Log?.LogInfo(
                        $"[ConfigurableLC] Patched {name}: 0x{vanilla:X2}->0x{replacement:X2} " +
                        $"@ method+0x{i:X}  {context}");
                }
            }

            int alreadyAtCap = 0;
            for (int i = 2; i < maxScan - 1; i++)
            {
                if (buf[i] != replacement) continue;
                if (!IsCmpImm8(buf, i)) continue;
                if (buf[i] == vanilla) continue; // this site matches vanilla in the pre-scan buffer -> we patch it ourselves
                if (!_patchedAddresses.Contains(IntPtr.Add(codePtr, i))) continue; // only our own patches count
                alreadyAtCap++;
            }

            if (patched == 0 && alreadyAtCap == 0)
            {
                ConfigurableLevelCapPlugin.Log?.LogWarning(
                    $"[ConfigurableLC] No cmp imm8=0x{vanilla:X2} found in {name}");
            }
            else if (patched == 0 && alreadyAtCap > 0)
            {
                ConfigurableLevelCapPlugin.Log?.LogInfo(
                    $"[ConfigurableLC] {name}: {alreadyAtCap} cmp site(s) already at 0x{replacement:X2} " +
                    $"(claimed by overlapping scan)");
            }

            return (patched, alreadyAtCap);
        }

        /// <summary>
        /// Returns a hex string of the bytes surrounding position 'pos' for diagnostic logging.
        /// </summary>
        private static string GetHexContext(byte[] buf, int pos)
        {
            int start = Math.Max(0, pos - 4);
            int end   = Math.Min(buf.Length - 1, pos + 4);
            var sb = new System.Text.StringBuilder("[");
            for (int j = start; j <= end; j++)
            {
                if (j == pos) sb.Append(">>>");
                sb.Append($"0x{buf[j]:X2}");
                if (j == pos) sb.Append("<<<");
                if (j < end) sb.Append(' ');
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Checks if the byte at position 'pos' in 'buf' is the immediate operand of a
        /// 'cmp r/m, imm8' instruction (opcode 0x83, reg field = 7).
        ///
        /// Handles all addressing modes:
        ///   83 /7 ib                — cmp reg, imm8              (e.g. 83 F8 19)
        ///   4X 83 /7 ib             — REX.W cmp reg, imm8       (e.g. 41 83 F8 19)
        ///   83 7C 24 disp8 ib       — cmp [rsp+disp8], imm8     (e.g. 83 7C 24 10 19)
        ///   83 BC 24 disp32 ib      — cmp [rsp+disp32], imm8    (e.g. 83 BC 24 00 01 00 00 19)
        ///   83 7D disp8 ib          — cmp [rbp+disp8], imm8     (e.g. 83 7D 10 19)
        ///   83 BD disp32 ib         — cmp [rbp+disp32], imm8    (e.g. 83 BD 00 01 00 00 19)
        ///   4X 83 7C 24 disp8 ib   — REX cmp [rsp+disp8], imm8
        ///   4X 83 BC 24 disp32 ib  — REX cmp [rsp+disp32], imm8
        ///   etc.
        /// </summary>
        private static bool IsCmpImm8(byte[] buf, int pos)
        {
            // Try all possible opcode positions from pos-8 to pos-2
            // (max instruction length for cmp r/m, imm8 is 9 bytes with REX+disp32+SIB)
            for (int opcodePos = pos - 8; opcodePos <= pos - 2; opcodePos++)
            {
                if (opcodePos < 0) continue;

                int p = opcodePos;

                // Check for optional REX prefix (0x40-0x4F)
                if ((buf[p] & 0xF0) == 0x40)
                {
                    p++;
                    if (p >= buf.Length) continue;
                }

                // Check opcode: must be 0x83 (cmp r/m, imm8)
                if (buf[p] != 0x83) continue;
                p++;
                if (p >= buf.Length) continue;

                // Read ModR/M byte
                byte modrm = buf[p];
                // reg field (bits 5-3) must be 7 (cmp opcode extension)
                if ((modrm & 0x38) != 0x38) continue;
                p++;
                if (p >= buf.Length) continue;

                int mod = (modrm >> 6) & 3;
                int rm  = modrm & 7;

                // Skip SIB byte if present (rm=4 and mod!=3)
                if (mod != 3 && rm == 4)
                {
                    p++; // skip SIB
                    if (p >= buf.Length) continue;
                }

                // Skip displacement bytes
                if (mod == 0 && rm == 5)
                    p += 4; // [rip+disp32] or [rbp+0]
                else if (mod == 1)
                    p += 1; // disp8
                else if (mod == 2)
                    p += 4; // disp32
                // mod == 3: register-direct, no displacement

                // Now p should point to the immediate byte
                if (p == pos)
                    return true;
            }

            return false;
        }
    }

    // ============================================================================
    // HARMONY PATCHES (belt-and-suspenders on top of the native fix)
    // ============================================================================

    // Robust override so EXP gain is blocked only at/above the raised cap.
    [HarmonyPatch(typeof(XP), nameof(XP.IsXPGainBlocked))]
    public static class Patch_XPGainBlocked
    {
        public static bool Prefix(Player player, ref bool __result)
        {
            if (player == null || player.Level == null) return true;

            int level = player.Level.Value;
            int cap = ConfigurableLevelCapPlugin.MaxLevelCap.Value;

            __result = level >= cap;
            return false;
        }
    }

    // Applies the configured EXP multiplier to all EXP sources.
    [HarmonyPatch(typeof(XP), nameof(XP.CalculateXPGained))]
    public static class Patch_ExpMultiplier
    {
        public static void Postfix(ref int __result)
        {
            float mult = ConfigurableLevelCapPlugin.ExpMultiplier.Value;
            if (mult > 1.0f)
                __result = (int)Math.Round(__result * mult);
        }
    }

    // Diagnostic: log the player's level when they spawn.
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    public static class Patch_PlayerAwake
    {
        public static void Postfix(Player __instance)
        {
            ConfigurableLevelCapPlugin.Log?.LogInfo(
                $"[ConfigurableLC] Player spawned: level={__instance.Level?.Value ?? -1}, " +
                $"cap={ConfigurableLevelCapPlugin.MaxLevelCap.Value}");
        }
    }
}
