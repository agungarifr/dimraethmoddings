using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.SystemMod
{
    /// <summary>
    /// [2026-09-27 00:00] New: Auto Backup Save (user request).
    /// Snapshot the game's save files into a timestamped folder every time the player quits the
    /// game (in-game "Exit Game" menu, Alt-F4, or any other quit path).
    ///
    /// Why this hook:
    ///   * The game saves on quit through SaveOnQuit: OnEnable subscribes HandleWantsToQuit to
    ///     UnityEngine.Application.wantsToQuit, and that handler runs SaveOnQuit.BeginQuitSequence()
    ///     (SaveWorldData -> Player.SaveGame -> SaveSystem.SaveBarrier -> ... -> SteamClient.Shutdown).
    ///   * Unity raises Application.wantsToQuit BEFORE Application.quitting, so by the time we react to
    ///     Application.quitting the game's save is already flushed to disk.
    ///   * Application.quitting is a plain managed Unity event, so it always fires - unlike a Harmony
    ///     patch on the private static SaveOnQuit.BeginQuitSequence, which IL2CPP inlined into its
    ///     callers so the patch never ran (that was the first two attempts; see notes below).
    ///   * Save files are tiny (<= ~30 KB each), so a synchronous copy at quit time is instant.
    ///
    /// The game itself only keeps .bak1/.bak2 plus a Recovery dir; this module adds a longer,
    /// timestamped history. Backups default to a sibling folder OUTSIDE the Steam Cloud-synced
    /// save root (Characters/Worlds/Settings each contain steam_autocloud.vdf). The mod never
    /// touches or restores the originals - restoring is a manual copy, by design.
    /// </summary>
    public class AutoBackupSaveModule : ModModuleBase
    {
        public static AutoBackupSaveModule Instance { get; private set; }

        public override string Category => "System";
        public override string Name => "Auto Backup Save";
        public override string Description => "Snapshot your save files to a timestamped folder every time you quit the game";

        public ConfigEntry<int> MaxBackups;
        public ConfigEntry<bool> IncludeSettings;
        public ConfigEntry<bool> SkipIfUnchanged;
        public ConfigEntry<string> BackupFolder;

        private const string DefaultFolderName = "Dimraeth_Backups";

        private string _status = "";

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "System.AutoBackupSave";

            Enabled = config.Bind(sec, "Enabled", true,
                "Enable Auto Backup Save (Default: true, Vanilla: false)");

            MaxBackups = config.Bind(sec, "MaxBackups", 10,
                "How many timestamped backups to keep; the oldest are deleted (Default: 10)");

            IncludeSettings = config.Bind(sec, "IncludeSettings", true,
                "Also back up Settings/SystemSettings.jrsf (Default: true)");

            SkipIfUnchanged = config.Bind(sec, "SkipIfUnchanged", true,
                "Do not create a backup when the save files are identical to the newest backup (Default: true)");

            BackupFolder = config.Bind(sec, "BackupFolder", "",
                "Folder that holds the backups. Empty = sibling 'Dimraeth_Backups' next to the save root (outside Steam Cloud).");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
            SubscribeQuitEvent();
        }

        private static bool _quitSubscribed;
        // [2026-09-27 20:25] Application.quitting is typed as Il2CppSystem.Action in the interop (a class
        // wrapping a native delegate, NOT a C# delegate), so a method group cannot be used directly.
        // Keep a static reference to the wrapper so the GC does not collect it while Unity still holds it.
        private static Il2CppSystem.Action _quitDelegate;

        // [2026-09-27 20:20] Primary quit trigger. Application.quitting is a managed Unity event that
        // always fires on exit (menu Exit, Alt-F4, Application.Quit, OS shutdown). The game's own save
        // runs earlier, inside Application.wantsToQuit, so our copy sees the final save. This replaces
        // relying on a Harmony patch of the private static BeginQuitSequence, which IL2CPP inlined so
        // it never fired in-game (the log showed zero backups across two quit sessions).
        private static void SubscribeQuitEvent()
        {
            if (_quitSubscribed) return;
            _quitSubscribed = true;
            try
            {
                _quitDelegate = (Il2CppSystem.Action)new System.Action(OnApplicationQuitting);
                Application.quitting += _quitDelegate;
                DimraethModPackPlugin.Log?.LogInfo("[AutoBackupSave] subscribed to Application.quitting.");
            }
            catch (Exception ex)
            {
                DimraethModPackPlugin.Log?.LogWarning($"[AutoBackupSave] could not subscribe to Application.quitting: {ex.Message}");
            }
        }

        private static void OnApplicationQuitting()
        {
            if (Instance == null || !Instance.IsEnabled) return;
            try
            {
                DimraethModPackPlugin.Log?.LogInfo("[AutoBackupSave] quit detected (Application.quitting).");
                Instance.RunBackup("quit");
            }
            catch (Exception ex)
            {
                DimraethModPackPlugin.Log?.LogWarning($"[AutoBackupSave] quitting hook failed: {ex.Message}");
            }
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawIntSpinner(x, curY, width, "Keep Backups", MaxBackups, 1, 1, 100, "Default: 10", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Include Settings", IncludeSettings, "Default: ON", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Skip If Unchanged", SkipIfUnchanged, "Default: ON", labelStyle, btnStyle);

            if (GUI.Button(new Rect(x, curY, 150f, 24f), "Backup Now", btnStyle))
            {
                RunBackup("manual");
            }
            curY += 28f;

            string path = "";
            try { path = ResolveBackupRoot(ResolveSaveRoot()); } catch { }
            GUI.Label(new Rect(x, curY, width, 18f), $"<color=#888888>Folder: {path}</color>", labelStyle);
            curY += 20f;

            if (!string.IsNullOrEmpty(_status))
            {
                GUI.Label(new Rect(x, curY, width, 20f), _status, labelStyle);
                curY += 22f;
            }

            return curY - y;
        }

        // ------------------------------------------------------------------
        // Backup core
        // ------------------------------------------------------------------

        private void RunBackup(string trigger)
        {
            try
            {
                string saveRoot = ResolveSaveRoot();
                if (saveRoot == null || !Directory.Exists(saveRoot))
                {
                    _status = "<color=#FF6666>Save folder not found.</color>";
                    return;
                }

                string backupRoot = ResolveBackupRoot(saveRoot);
                Directory.CreateDirectory(backupRoot);

                // [2026-09-27 20:20] Marker proves the hook fired even when the BepInEx logger is
                // already disposed during shutdown. Best-effort only; never fails the backup.
                try
                {
                    File.WriteAllText(Path.Combine(backupRoot, "_last_run.txt"),
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} trigger={trigger}{Environment.NewLine}");
                }
                catch { }

                var sourceDirs = new List<string>();
                foreach (string sub in new[] { "Characters", "Worlds" })
                {
                    string d = Path.Combine(saveRoot, sub);
                    if (Directory.Exists(d)) sourceDirs.Add(d);
                }
                if (IncludeSettings.Value)
                {
                    string d = Path.Combine(saveRoot, "Settings");
                    if (Directory.Exists(d)) sourceDirs.Add(d);
                }
                if (sourceDirs.Count == 0)
                {
                    _status = "<color=#FF6666>No save files found.</color>";
                    return;
                }

                string signature = ComputeSignature(sourceDirs);

                string latest = GetNewestSnapshot(backupRoot);
                if (SkipIfUnchanged.Value && latest != null)
                {
                    string sigFile = Path.Combine(latest, "_signature.txt");
                    try
                    {
                        if (File.Exists(sigFile) && File.ReadAllText(sigFile) == signature)
                        {
                            _status = "<color=#888888>Unchanged since last backup - skipped.</color>";
                            return;
                        }
                    }
                    catch { }
                }

                string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string dest = Path.Combine(backupRoot, stamp);
                int suffix = 1;
                while (Directory.Exists(dest))
                    dest = Path.Combine(backupRoot, stamp + "_" + suffix++);
                Directory.CreateDirectory(dest);

                int copied = 0;
                foreach (string src in sourceDirs)
                {
                    string dstDir = Path.Combine(dest, Path.GetFileName(src));
                    Directory.CreateDirectory(dstDir);
                    foreach (string file in Directory.GetFiles(src))
                    {
                        if (IsSkippedFile(file)) continue;
                        CopyFileShared(file, Path.Combine(dstDir, Path.GetFileName(file)));
                        copied++;
                    }
                }
                File.WriteAllText(Path.Combine(dest, "_signature.txt"), signature);

                Prune(backupRoot);

                _status = $"<color=#55FF55>Backup created: {stamp} ({copied} files).</color>";
                DimraethModPackPlugin.Log?.LogInfo($"[AutoBackupSave] backup '{Path.GetFileName(dest)}' ({copied} files) -> {dest}");
            }
            catch (Exception ex)
            {
                _status = $"<color=#FF6666>Backup failed: {ex.Message}</color>";
                DimraethModPackPlugin.Log?.LogWarning($"[AutoBackupSave] backup failed: {ex}");
            }
        }

        private void Prune(string backupRoot)
        {
            int keep = Math.Max(1, MaxBackups.Value);
            var dirs = new List<string>(Directory.GetDirectories(backupRoot));
            // Timestamped names sort chronologically; descending = newest first.
            dirs.Sort((a, b) => string.CompareOrdinal(Path.GetFileName(b), Path.GetFileName(a)));
            for (int i = keep; i < dirs.Count; i++)
            {
                try { Directory.Delete(dirs[i], true); }
                catch (Exception ex)
                {
                    DimraethModPackPlugin.Log?.LogWarning($"[AutoBackupSave] could not delete old backup '{dirs[i]}': {ex.Message}");
                }
            }
        }

        // ------------------------------------------------------------------
        // Paths, hashing, IO helpers
        // ------------------------------------------------------------------

        private static string ResolveSaveRoot()
        {
            try
            {
                string playerDir = SaveFiles.PlayerDir;
                if (!string.IsNullOrEmpty(playerDir))
                {
                    var info = new DirectoryInfo(playerDir);
                    if (info.Parent != null) return info.Parent.FullName;
                }
            }
            catch { }
            return Application.persistentDataPath;
        }

        private static string ResolveBackupRoot(string saveRoot)
        {
            string custom = AutoBackupSaveModule.Instance?.BackupFolder?.Value;
            if (!string.IsNullOrWhiteSpace(custom)) return custom.Trim();

            var parent = new DirectoryInfo(saveRoot).Parent;
            string baseDir = parent != null ? parent.FullName : saveRoot;
            return Path.Combine(baseDir, DefaultFolderName);
        }

        private static string GetNewestSnapshot(string backupRoot)
        {
            string newest = null;
            foreach (string d in Directory.GetDirectories(backupRoot))
            {
                if (newest == null || string.CompareOrdinal(Path.GetFileName(d), Path.GetFileName(newest)) > 0)
                    newest = d;
            }
            return newest;
        }

        /// <summary>FNV-1a 64 over the sorted (name + content) of every backed-up file.</summary>
        private static string ComputeSignature(List<string> sourceDirs)
        {
            var files = new List<string>();
            foreach (string dir in sourceDirs)
                foreach (string f in Directory.GetFiles(dir))
                    if (!IsSkippedFile(f)) files.Add(f);
            files.Sort(StringComparer.OrdinalIgnoreCase);

            ulong hash = 14695981039346656037UL; // FNV offset basis
            const ulong prime = 1099511628211UL;
            foreach (string f in files)
            {
                foreach (char c in Path.GetFileName(f))
                {
                    hash ^= c;
                    hash *= prime;
                }
                try
                {
                    byte[] bytes = ReadAllBytesShared(f);
                    foreach (byte b in bytes)
                    {
                        hash ^= b;
                        hash *= prime;
                    }
                }
                catch { }
            }
            return hash.ToString("X16");
        }

        private static bool IsSkippedFile(string path)
        {
            return path.EndsWith(".vdf", StringComparison.OrdinalIgnoreCase);
        }

        private static byte[] ReadAllBytesShared(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var buffer = new byte[fs.Length];
                int read = 0;
                while (read < buffer.Length)
                {
                    int got = fs.Read(buffer, read, buffer.Length - read);
                    if (got <= 0) break;
                    read += got;
                }
                return buffer;
            }
        }

        private static void CopyFileShared(string src, string dst)
        {
            using (var input = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var output = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None))
                input.CopyTo(output);
        }

        public static class Patches
        {
            /* [2026-09-27 16:50] Obsolete hook: postfix on SaveOnQuit.TryFinalize. It never fired on a
               real exit (log showed no backup). From the game's ISIL (cpp2il_isil_out/IsilDump/
               Assembly-CSharp/SaveOnQuit.txt), TryFinalize is only called by OnApplicationFocus,
               OnApplicationPause and OnLowMemory - partial saves, not the quit path. The real
               save-then-quit path is BeginQuitSequence() (see the active patch below). Kept commented
               per repo rule.
            [HarmonyPatch(typeof(SaveOnQuit), "TryFinalize")]
            [HarmonyPostfix]
            public static void Postfix_TryFinalize()
            {
                if (Instance == null || !Instance.IsEnabled) return;
                try
                {
                    if (!SaveOnQuit.IsQuitting) return;
                    Instance.RunBackup();
                }
                catch (Exception ex)
                {
                    DimraethModPackPlugin.Log?.LogWarning($"[AutoBackupSave] quit hook failed: {ex.Message}");
                }
            }
            */

            // [2026-09-27 16:50] Correct hook: BeginQuitSequence() is the game's real save-then-quit path
            // (in-game menu Exit, Alt-F4 via HandleWantsToQuit, and OnApplicationQuit). By the time this
            // postfix runs the world + player have been saved and SaveSystem.SaveBarrier() has flushed
            // the background writer, so the save files are final. No IsQuitting gate is needed - the
            // method itself only runs while the game is quitting.
            [HarmonyPatch(typeof(SaveOnQuit), "BeginQuitSequence")]
            [HarmonyPostfix]
            public static void Postfix_BeginQuitSequence()
            {
                if (Instance == null || !Instance.IsEnabled) return;
                try
                {
                    DimraethModPackPlugin.Log?.LogInfo("[AutoBackupSave] quit detected (BeginQuitSequence).");
                    Instance.RunBackup("quit");
                }
                catch (Exception ex)
                {
                    DimraethModPackPlugin.Log?.LogWarning($"[AutoBackupSave] quit hook failed: {ex.Message}");
                }
            }
        }
    }
}
