using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using System.Collections.Generic;
using UnityEngine;
using PlayerRoles;
using System.Linq;
using Exiled.API.Features.Items;
using Exiled.API.Enums;
using Exiled.API.Extensions;

namespace AntiAFK
{
    public class EventHandlers
    {
        // --- State ---
        private readonly Dictionary<Player, Vector3> _lastPositions = new Dictionary<Player, Vector3>();
        private readonly Dictionary<Player, Quaternion> _lastRotations = new Dictionary<Player, Quaternion>();
        private readonly Dictionary<Player, float> _afkTimes = new Dictionary<Player, float>();
        private CoroutineHandle _afkCoroutine;

        private const float RotationThresholdDegrees = 10f;

        private void TouchActivity(Player player)
        {
            if (player is null) return;
            if (!_afkTimes.ContainsKey(player)) return;

            _afkTimes[player] = 0f;
            _lastPositions[player] = player.Position;
            _lastRotations[player] = player.Rotation;
        }

        // --- Registration ---

        public void Register()
        {
            Exiled.Events.Handlers.Player.Verified += OnVerified;
            Exiled.Events.Handlers.Player.Left += OnLeft;
            Exiled.Events.Handlers.Player.UsingItem += OnUsingItem;
            Exiled.Events.Handlers.Player.UsingItemCompleted += OnUsingItemCompleted;
            Exiled.Events.Handlers.Player.ChangingItem += OnChangingItem;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.PickingUpItem += OnPickingUpItem;
            Exiled.Events.Handlers.Player.Shooting += OnShooting;
            Exiled.Events.Handlers.Player.ReloadingWeapon += OnReloadingWeapon;
            Exiled.Events.Handlers.Player.VoiceChatting += OnVoiceChatting;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;

            if (!_afkCoroutine.IsRunning)
                _afkCoroutine = Timing.RunCoroutine(AfkCheckLoop());
        }

        public void Unregister()
        {
            Exiled.Events.Handlers.Player.Verified -= OnVerified;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Player.UsingItem -= OnUsingItem;
            Exiled.Events.Handlers.Player.UsingItemCompleted -= OnUsingItemCompleted;
            Exiled.Events.Handlers.Player.ChangingItem -= OnChangingItem;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.PickingUpItem -= OnPickingUpItem;
            Exiled.Events.Handlers.Player.Shooting -= OnShooting;
            Exiled.Events.Handlers.Player.ReloadingWeapon -= OnReloadingWeapon;
            Exiled.Events.Handlers.Player.VoiceChatting -= OnVoiceChatting;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;

            if (_afkCoroutine.IsRunning)
                Timing.KillCoroutines(_afkCoroutine);
            
            _lastPositions.Clear();
            _lastRotations.Clear();
            _afkTimes.Clear();
        }

        // --- Lifecycle ---

        private void OnWaitingForPlayers()
        {
            if (_afkCoroutine.IsRunning)
                Timing.KillCoroutines(_afkCoroutine);
            
            _lastPositions.Clear();
            _lastRotations.Clear();
            _afkTimes.Clear();
        }

        private void OnRoundStarted()
        {
            if (_afkCoroutine.IsRunning)
                Timing.KillCoroutines(_afkCoroutine);

            _afkCoroutine = Timing.RunCoroutine(AfkCheckLoop());
        }

        // --- Player Events ---

        private void OnVerified(VerifiedEventArgs ev)
        {
            if (ev?.Player is null) return;

            if (!_afkTimes.ContainsKey(ev.Player))
            {
                _afkTimes[ev.Player] = 0f;
                _lastPositions[ev.Player] = ev.Player.Position;
                _lastRotations[ev.Player] = ev.Player.Rotation;
            }
        }

        private void OnLeft(LeftEventArgs ev)
        {
            if (ev?.Player is null) return;

            if (_afkTimes.ContainsKey(ev.Player))
                _afkTimes.Remove(ev.Player);
            
            if (_lastPositions.ContainsKey(ev.Player))
                _lastPositions.Remove(ev.Player);

            if (_lastRotations.ContainsKey(ev.Player))
                _lastRotations.Remove(ev.Player);
        }

        private void OnUsingItem(UsingItemEventArgs ev)
        {
            if (ev?.Player is null) return;
            TouchActivity(ev.Player);
        }

        private void OnUsingItemCompleted(UsingItemCompletedEventArgs ev)
        {
            if (ev?.Player is null) return;
            TouchActivity(ev.Player);
        }

        private void OnChangingItem(ChangingItemEventArgs ev)
        {
            if (ev?.Player is null) return;
            TouchActivity(ev.Player);
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            if (ev?.Player is null) return;
            TouchActivity(ev.Player);
        }

        private void OnPickingUpItem(PickingUpItemEventArgs ev)
        {
            if (ev?.Player is null) return;
            TouchActivity(ev.Player);
        }

        private void OnShooting(ShootingEventArgs ev)
        {
            if (ev?.Player is null) return;
            TouchActivity(ev.Player);
        }

        private void OnReloadingWeapon(ReloadingWeaponEventArgs ev)
        {
            if (ev?.Player is null) return;
            TouchActivity(ev.Player);
        }

        private void OnVoiceChatting(VoiceChattingEventArgs ev)
        {
            if (ev?.Player is null) return;
            if (!ev.IsAllowed) return;
            TouchActivity(ev.Player);
        }

        // --- Core Logic (AFK Loop) ---

        private IEnumerator<float> AfkCheckLoop()
        {
            while (Round.IsStarted)
            {
                float interval = Plugin.Instance.Config.HudInterval;

                foreach (Player player in Player.List)
                {
                    if (player == null || !player.IsAlive)
                        continue;

                    if (player.Role.Type == RoleTypeId.Spectator || player.Role.Type == RoleTypeId.Overwatch || player.Role.Type == RoleTypeId.None || player.Role.Type == RoleTypeId.Tutorial || player.Role.Type == RoleTypeId.Scp079)
                        continue;

                    if (!_lastPositions.ContainsKey(player))
                    {
                        _lastPositions[player] = player.Position;
                        _lastRotations[player] = player.Rotation;
                        _afkTimes[player] = 0f;
                        continue;
                    }

                    if (Vector3.Distance(player.Position, _lastPositions[player]) > 0.1f)
                    {
                        _lastPositions[player] = player.Position;
                        _lastRotations[player] = player.Rotation;
                        _afkTimes[player] = 0f;
                    }
                    else
                    {
                        if (!_lastRotations.TryGetValue(player, out var lastRot))
                        {
                            _lastRotations[player] = player.Rotation;
                            _afkTimes[player] = 0f;
                        }
                        else
                        {
                            float angle = Quaternion.Angle(lastRot, player.Rotation);
                            if (angle >= RotationThresholdDegrees)
                            {
                                _lastRotations[player] = player.Rotation;
                                _afkTimes[player] = 0f;
                            }
                            else
                            {
                                _afkTimes[player] += interval;
                            }
                        }
                    }

                    float remainingTime = Plugin.Instance.Config.AfkTime - _afkTimes[player];

                    if (remainingTime <= 0)
                    {
                        ReplacePlayer(player);
                        continue;
                    }

                    if (remainingTime <= Plugin.Instance.Config.WarningTime)
                    {
                        ShowWarning(player, remainingTime);
                    }
                }

                yield return Timing.WaitForSeconds(interval);
            }
        }

        // --- Helpers ---

        private void ShowWarning(Player player, float remainingTime)
        {
            string timeStr = string.Format("{0:00}:{1:00}", (int)remainingTime / 60, (int)remainingTime % 60);
            string message = Plugin.Instance.Translation.WarningMessage.Replace("{time}", timeStr);
            string styled = $"<size=35>{message}</size>";
            
            player.ShowHint(styled, Plugin.Instance.Config.HudInterval + 0.1f);
        }

        private void ReplacePlayer(Player afkPlayer)
        {
            if (afkPlayer is null || afkPlayer.Role.Type == RoleTypeId.Tutorial)
                return;

            Player spectator = Player.List.FirstOrDefault(p => p.Role.Type == RoleTypeId.Spectator && !p.IsOverwatchEnabled);

            if (spectator != null)
            {
                RoleTypeId oldRole = afkPlayer.Role.Type;
                Vector3 oldPos = afkPlayer.Position;
                float health = afkPlayer.Health;
                List<Item> items = afkPlayer.Items.ToList();
                
                var ammo = new Dictionary<AmmoType, ushort>();
                foreach (var kvp in afkPlayer.Ammo)
                {
                    AmmoType ammoType = kvp.Key.GetAmmoType();
                    if (ammoType != AmmoType.None)
                        ammo[ammoType] = kvp.Value;
                }
                
                afkPlayer.ClearInventory();
                afkPlayer.Role.Set(RoleTypeId.Spectator);
                afkPlayer.ShowHint(Plugin.Instance.Translation.ReplacedMessage, 5f);

                spectator.Role.Set(oldRole);
                
                Timing.CallDelayed(0.5f, () =>
                {
                    spectator.Position = oldPos;
                    spectator.Health = health;
                    spectator.ClearInventory();
                    
                    foreach (var item in items)
                        spectator.AddItem(item.Type);

                    spectator.SetAmmo(ammo);

                    CopyEffect(afkPlayer, spectator, EffectType.Scp207);
                    CopyEffect(afkPlayer, spectator, EffectType.Invigorated);
                    CopyEffect(afkPlayer, spectator, EffectType.Vitality);
                    CopyEffect(afkPlayer, spectator, EffectType.Scp1853);
                    CopyEffect(afkPlayer, spectator, EffectType.MovementBoost);
                    CopyEffect(afkPlayer, spectator, EffectType.RainbowTaste);
                    
                    spectator.ShowHint(Plugin.Instance.Translation.ReplacementMessage, 5f);
                });
            }
            else
            {
                afkPlayer.Role.Set(RoleTypeId.Spectator);
                afkPlayer.ShowHint(Plugin.Instance.Translation.ReplacedMessage, 5f);
            }

            _afkTimes[afkPlayer] = 0f;
            _lastPositions[afkPlayer] = afkPlayer.Position;
        }

        private void CopyEffect(Player from, Player to, EffectType effectType)
        {
            if (from is null || to is null)
                return;

            if (!from.TryGetEffect(effectType, out var effect) || effect is null || !effect.IsEnabled)
                return;

            to.EnableEffect(effectType, effect.Intensity, effect.Duration, false);
        }
    }
}
