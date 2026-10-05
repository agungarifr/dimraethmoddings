using System;
using Il2CppInterop.Runtime.InteropTypes;

namespace DimraethModPack.Core
{
    /// <summary>
    /// [2026-09-28 10:35] Crash-safe "is this the local player?" test.
    ///
    /// `ObjectsCommon.IsPlayer` is a native FIELD read at a fixed offset. When the game
    /// invokes a patched Formula method during the character-loading transition the
    /// argument can be an interop wrapper whose native pointer is null or already
    /// destroyed, and that field read raises an AccessViolationException. An
    /// AccessViolationException is a fatal, UNCATCHABLE error in .NET, so the old
    /// `if (obj != null) obj.IsPlayer` guard hard-killed the process - the try/catch
    /// around it never ran. Confirmed 2026-09-28 in BepInEx/ErrorLog.log:
    ///   ObjectsCommon.get_IsPlayer() &lt;- CustomStatModule.IsTargetPlayer
    ///   &lt;- Postfix_CalculateBaseSpellHaste.
    ///
    /// This helper never invokes anything on the candidate object. It only compares the
    /// candidate's raw pointer against the live local player (DataStorage.Singleton.Player),
    /// so a null/stale pointer can no longer reach a native field accessor.
    /// </summary>
    public static class PlayerIdentity
    {
        public static bool IsLocalPlayer(Il2CppObjectBase obj)
        {
            if (obj == null) return false;
            try
            {
                IntPtr ptr = obj.Pointer;
                if (ptr == IntPtr.Zero) return false;

                IntPtr local = LocalPlayerPtr();
                return local != IntPtr.Zero && local == ptr;
            }
            catch { return false; }
        }

        /// <summary>
        /// Live local-player pointer, or IntPtr.Zero when unavailable. Reading the field
        /// does not dereference the returned player, so this is safe during transitions.
        /// </summary>
        public static IntPtr LocalPlayerPtr()
        {
            try
            {
                DataStorage storage = DataStorage.Singleton;
                if (storage == null) return IntPtr.Zero;
                Player local = storage.Player;
                return local != null ? local.Pointer : IntPtr.Zero;
            }
            catch { return IntPtr.Zero; }
        }
    }
}
