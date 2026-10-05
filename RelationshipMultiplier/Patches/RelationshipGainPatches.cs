using System;
using HarmonyLib;

namespace Dimraeth.RelationshipMultiplier.Patches
{
    /// <summary>
    /// Melipatgandakan relationship gain yang dihitung oleh NPCDialogues.
    ///
    /// Target dibuktikan dari interop (Assembly-CSharp.dll):
    ///   NPCDialogues.GainFor(RelationshipData gains, RelationshipStat stat) -> int
    ///   NPCDialogues.TotalRelationshipGain(RelationshipData gains) -> int
    ///
    /// Karena RelationshipBonus bersifat per-entri percakapan (bukan per-opsi),
    /// target terbaik adalah method yang mengubah nilainya menjadi gain nyata.
    /// </summary>
    [HarmonyPatch]
    public static class RelationshipGainPatches
    {
        // GainFor: gain per komponen relasi (Opinion / Friendship / Romance / ValueAlignment)
        [HarmonyPatch(typeof(NPCDialogues), nameof(NPCDialogues.GainFor))]
        [HarmonyPostfix]
        static void MultiplyGainFor(ref int __result)
        {
            float mult = RelationshipMultiplierPlugin.GainMultiplier?.Value ?? 1f;
            if (mult != 1f)
                __result = (int)Math.Round(__result * mult);
        }

        // TotalRelationshipGain: jumlah total gain (mis. untuk UI / status)
        [HarmonyPatch(typeof(NPCDialogues), nameof(NPCDialogues.TotalRelationshipGain))]
        [HarmonyPostfix]
        static void MultiplyTotalGain(ref int __result)
        {
            float mult = RelationshipMultiplierPlugin.TotalGainMultiplier?.Value ?? 1f;
            if (mult != 1f)
                __result = (int)Math.Round(__result * mult);
        }
    }
}
