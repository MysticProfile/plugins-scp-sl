using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using PlayerEvents = Exiled.Events.Handlers.Player;
using MEC;
using Scp914Events = Exiled.Events.Handlers.Scp914;
using System;
using System.Collections.Generic;

namespace Scp914VeryFineEffects
{
    internal static class EventHandlers
    {
        // --- State ---
        private static Config Config => Plugin.Instance.Config;

        private static readonly HashSet<string> ActiveEffectOwners = new HashSet<string>();
        private static readonly Dictionary<string, float> EffectEndTimes = new Dictionary<string, float>();
        private static readonly Dictionary<string, CoroutineHandle> StaminaCoroutines = new Dictionary<string, CoroutineHandle>();
        private static readonly Dictionary<string, CoroutineHandle> BleedingFixCoroutines = new Dictionary<string, CoroutineHandle>();

        // --- Registration ---

        public static void Register()
        {
            Scp914Events.UpgradingPlayer += OnUpgradingPlayer;
            PlayerEvents.UsingItemCompleted += OnUsingItemCompleted;
            PlayerEvents.Healed += OnHealed;
            PlayerEvents.Left += OnLeft;
            PlayerEvents.Died += OnDied;
            PlayerEvents.ChangingRole += OnChangingRole;
        }

        public static void Unregister()
        {
            Scp914Events.UpgradingPlayer -= OnUpgradingPlayer;
            PlayerEvents.UsingItemCompleted -= OnUsingItemCompleted;
            PlayerEvents.Healed -= OnHealed;
            PlayerEvents.Left -= OnLeft;
            PlayerEvents.Died -= OnDied;
            PlayerEvents.ChangingRole -= OnChangingRole;

            ActiveEffectOwners.Clear();
            EffectEndTimes.Clear();
            
            foreach (var handle in StaminaCoroutines.Values)
                if (handle.IsRunning) Timing.KillCoroutines(handle);
            StaminaCoroutines.Clear();

            foreach (var handle in BleedingFixCoroutines.Values)
                if (handle.IsRunning) Timing.KillCoroutines(handle);
            BleedingFixCoroutines.Clear();
        }

        // --- Events ---

        private static void OnUpgradingPlayer(UpgradingPlayerEventArgs ev)
        {
            try
            {
                if (!Config.IsEnabled || ev?.Player is null)
                    return;

                if (ev.KnobSetting != global::Scp914.Scp914KnobSetting.VeryFine)
                    return;

                float duration = Config.DurationSeconds;
                if (duration <= 0f)
                    return;

                ev.Player.EnableEffects(new[] { EffectType.Scp207, EffectType.Bleeding }, duration, Config.AddDurationIfActive);

                string userId = ev.Player.UserId;
                if (!string.IsNullOrEmpty(userId))
                {
                    ActiveEffectOwners.Add(userId);

                    float end = Timing.LocalTime + duration;
                    if (Config.AddDurationIfActive && EffectEndTimes.TryGetValue(userId, out float existingEnd) && existingEnd > Timing.LocalTime)
                        end = existingEnd + duration;
                    
                    EffectEndTimes[userId] = end;
                }

                if (Config.Debug)
                    Log.Debug($"[Scp914VeryFineEffects] Applied Scp207 + Bleeding to {ev.Player.Nickname} for {duration}s");
            }
            catch (Exception e)
            {
                if (Config.Debug)
                    Log.Error($"[Scp914VeryFineEffects] Exception in OnUpgradingPlayer: {e}");
            }
        }

        private static void OnHealed(HealedEventArgs ev)
        {
            try
            {
                if (!Config.IsEnabled || ev?.Player is null)
                    return;

                string userId = ev.Player.UserId;
                if (string.IsNullOrEmpty(userId) || !ActiveEffectOwners.Contains(userId))
                    return;

                if (!EffectEndTimes.TryGetValue(userId, out float end))
                    return;

                float remaining = end - Timing.LocalTime;
                if (remaining <= 0f)
                    return;

                StartBleedingFix(ev.Player, remaining);
            }
            catch (Exception e)
            {
                if (Config.Debug)
                    Log.Error($"[Scp914VeryFineEffects] Exception in OnHealed: {e}");
            }
        }

        private static void OnUsingItemCompleted(UsingItemCompletedEventArgs ev)
        {
            try
            {
                if (!Config.IsEnabled || ev?.Player is null || ev.Usable is null)
                    return;

                if (ev.Usable.Type != ItemType.SCP500)
                    return;

                string userId = ev.Player.UserId;
                if (string.IsNullOrEmpty(userId) || !ActiveEffectOwners.Contains(userId))
                    return;

                ev.Player.DisableEffects(new[] { EffectType.Scp207, EffectType.Bleeding });
                ActiveEffectOwners.Remove(userId);
                EffectEndTimes.Remove(userId);

                StartInfiniteStamina(ev.Player, 10f);

                if (Config.Debug)
                    Log.Debug($"[Scp914VeryFineEffects] {ev.Player.Nickname} consumed SCP-500: removed effects and granted infinite stamina for 10s");
            }
            catch (Exception e)
            {
                if (Config.Debug)
                    Log.Error($"[Scp914VeryFineEffects] Exception in OnUsingItemCompleted: {e}");
            }
        }

        private static void OnLeft(LeftEventArgs ev) => Cleanup(ev?.Player);
        private static void OnDied(DiedEventArgs ev) => Cleanup(ev?.Player);
        private static void OnChangingRole(ChangingRoleEventArgs ev) => Cleanup(ev?.Player);

        // --- Logic Helpers ---

        private static void Cleanup(Player player)
        {
            if (player is null) return;

            string userId = player.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            ActiveEffectOwners.Remove(userId);
            EffectEndTimes.Remove(userId);
            StopInfiniteStamina(userId);
            StopBleedingFix(userId);
        }

        private static void StartBleedingFix(Player player, float remainingSeconds)
        {
            string userId = player?.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            StopBleedingFix(userId);
            BleedingFixCoroutines[userId] = Timing.RunCoroutine(BleedingFixCoroutine(player, remainingSeconds));
        }

        private static void StopBleedingFix(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            if (BleedingFixCoroutines.TryGetValue(userId, out CoroutineHandle handle))
            {
                if (handle.IsRunning) Timing.KillCoroutines(handle);
                BleedingFixCoroutines.Remove(userId);
            }
        }

        private static void StartInfiniteStamina(Player player, float seconds)
        {
            string userId = player?.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            StopInfiniteStamina(userId);
            StaminaCoroutines[userId] = Timing.RunCoroutine(InfiniteStaminaCoroutine(player, seconds));
        }

        private static void StopInfiniteStamina(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            if (StaminaCoroutines.TryGetValue(userId, out CoroutineHandle handle))
            {
                if (handle.IsRunning) Timing.KillCoroutines(handle);
                StaminaCoroutines.Remove(userId);
            }
        }

        // --- Coroutines ---

        private static IEnumerator<float> BleedingFixCoroutine(Player player, float remainingSeconds)
        {
            string userId = player?.UserId;
            if (player is null || string.IsNullOrEmpty(userId))
                yield break;

            float end = Timing.LocalTime + remainingSeconds;
            // Try fix bleeding missing after healing a few times
            for (int i = 0; i < 5; i++)
            {
                yield return Timing.WaitForSeconds(0.1f);

                if (player is null || !player.IsConnected || !player.IsAlive || !ActiveEffectOwners.Contains(userId))
                    yield break;

                float remaining = end - Timing.LocalTime;
                if (remaining <= 0f)
                    yield break;

                bool hasBleeding = false;
                foreach (var effect in player.ActiveEffects)
                {
                    if (effect.TryGetEffectType(out EffectType effectType) && effectType == EffectType.Bleeding)
                    {
                        hasBleeding = true;
                        break;
                    }
                }

                if (!hasBleeding)
                    player.EnableEffect(EffectType.Bleeding, remaining, false);
            }

            BleedingFixCoroutines.Remove(userId);
        }

        private static IEnumerator<float> InfiniteStaminaCoroutine(Player player, float seconds)
        {
            string userId = player?.UserId;
            if (player is null || string.IsNullOrEmpty(userId))
                yield break;

            bool previous = player.IsUsingStamina;
            player.IsUsingStamina = false;
            player.ResetStamina();

            float end = Timing.LocalTime + seconds;
            while (Timing.LocalTime < end)
            {
                if (player is null || !player.IsConnected || !player.IsAlive)
                    yield break;

                player.ResetStamina();
                yield return Timing.WaitForSeconds(0.25f);
            }

            if (player != null && player.IsConnected)
                player.IsUsingStamina = previous;

            StaminaCoroutines.Remove(userId);
        }
    }
}
