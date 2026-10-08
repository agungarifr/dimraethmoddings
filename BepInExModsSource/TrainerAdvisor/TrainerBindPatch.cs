using System;
using HarmonyLib;
using UnityEngine;

namespace DimraethTrainerAdvisor
{
    // PlayerUIView.Bind(Player player) is the public entry point every player UI view runs when
    // it is bound; NPCTrainView.OnBind (the trainer) is called from inside it. We postfix Bind,
    // filter to the trainer view, and attach (or reuse) the advisor panel on the view's
    // _panelRoot. We only ever read player state.
    internal static class TrainerBindPatch
    {
        public static void Postfix(PlayerUIView __instance, Player player)
        {
            try
            {
                if (!Plugin.Enabled) return;
                if (__instance == null || player == null) return;
                if (!(__instance is NPCTrainView view)) return;

                GameObject root = null;
                try { root = view._panelRoot; } catch { }
                if (root == null) root = view.gameObject;
                if (root == null) return;

                var panel = root.GetComponent<TrainerAdvisorPanel>();
                if (panel == null) panel = root.AddComponent<TrainerAdvisorPanel>();
                panel.Configure(view, player);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Trainer advisor attach failed: {ex}");
            }
        }
    }
}
