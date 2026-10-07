using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomItems
{
    // [2026-10-07] "Restore X% of max Y over Z seconds" HoT for custom edibles
    // (Satay Madura: 50% of max Health/Stamina/Concentration over 1800s).
    //
    // Why a custom tick instead of vanilla Effect entries: the game's EffectEntry ->
    // Effects.AddEffectToObject path feeds Formulas.CalculateXRegeneration with
    // Multiplier/Additive semantics that cannot express "exactly 50% of max over
    // 30 minutes". This tracker computes the pool totals at consume time and writes
    // them in per frame through the same public API vanilla ApplyConsumption uses
    // (ObjectsCommon.SetHealth/SetStamina/SetConcentration).

    public class RegenOverTime
    {
        public Player Player;
        public ItemDef Def;
        public float RemainingSeconds;
        public float DurationSeconds;
        public float HealthPerSecond;
        public float StaminaPerSecond;
        public float ConcentrationPerSecond;
    }

    public static class RegenOverTimeTracker
    {
        static readonly List<RegenOverTime> _active = new();

        public static void Add(Player player, ItemDef def, Action<string> log, Action<string> warn)
        {
            var rot = def.Edible?.RegenOverTime;
            if (rot == null || rot.DurationSeconds <= 0f) return;
            if (player == null) { warn("RegenOverTime: no player to attach buff."); return; }

            float duration = rot.DurationSeconds;
            var buff = new RegenOverTime
            {
                Player = player,
                Def = def,
                DurationSeconds = duration,
                RemainingSeconds = duration,
                // Rate is fixed at consume time: fraction of CURRENT max spread over the duration.
                HealthPerSecond = rot.HealthFraction * SafeMax(player, 0) / duration,
                StaminaPerSecond = rot.StaminaFraction * SafeMax(player, 1) / duration,
                ConcentrationPerSecond = rot.ConcentrationFraction * SafeMax(player, 2) / duration,
            };

            // AddOrRefresh semantics (same as Passives.AddOrRefresh*): eating again refreshes.
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Def == def && _active[i].Player == player) _active.RemoveAt(i);
            }
            _active.Add(buff);

            log($"RegenOverTime started for '{def.Name}': " +
                $"HP {buff.HealthPerSecond:0.###}/s, STA {buff.StaminaPerSecond:0.###}/s, " +
                $"CON {buff.ConcentrationPerSecond:0.###}/s over {duration}s.");
        }

        public static void Tick(float dt)
        {
            if (_active.Count == 0 || dt <= 0f) return;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var buff = _active[i];
                try
                {
                    var player = buff.Player;
                    if (player == null || player.Pointer == IntPtr.Zero)
                    {
                        _active.RemoveAt(i);
                        continue;
                    }

                    buff.RemainingSeconds -= dt;
                    float step = Math.Min(dt, buff.RemainingSeconds + dt);

                    if (buff.HealthPerSecond > 0f)
                        player.SetHealth(Math.Min(player.Health.Value + buff.HealthPerSecond * step, SafeMax(player, 0)));
                    if (buff.StaminaPerSecond > 0f)
                        player.SetStamina(Math.Min(player.Stamina.Value + buff.StaminaPerSecond * step, SafeMax(player, 1)));
                    if (buff.ConcentrationPerSecond > 0f)
                        player.SetConcentration(Math.Min(player.Concentration.Value + buff.ConcentrationPerSecond * step, SafeMax(player, 2)));

                    if (buff.RemainingSeconds <= 0f) _active.RemoveAt(i);
                }
                catch (Exception)
                {
                    // Player object can be destroyed on scene change; drop the buff silently.
                    _active.RemoveAt(i);
                }
            }
        }

        public static void Clear() => _active.Clear();

        static float SafeMax(ObjectsCommon obj, int pool)
        {
            try
            {
                return pool switch
                {
                    0 => Formulas.CalculateMaxHealth(obj),
                    1 => Formulas.CalculateMaxStamina(obj),
                    _ => Formulas.CalculateMaxConcentration(obj),
                };
            }
            catch { return 1f; }
        }
    }

    // [2026-10-07] One Il2Cpp MonoBehaviour ticks everything per frame. Pattern copied
    // from RenosUtilitiesSource MinimapBehaviour (ClassInjector + DontDestroyOnLoad).
    public class CustomItemsTicker : MonoBehaviour
    {
        public CustomItemsTicker(IntPtr ptr) : base(ptr) { }

        public static CustomItemsTicker Instance;

        void Update()
        {
            try
            {
                RegenOverTimeTracker.Tick(Time.deltaTime);
                CustomItemsPlugin.PumpRegistration();
            }
            catch (Exception ex)
            {
                CustomItemsPlugin.LogOnce("ticker error: " + ex.Message);
            }
        }
    }
}
