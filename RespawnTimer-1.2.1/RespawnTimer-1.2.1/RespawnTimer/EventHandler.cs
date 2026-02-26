using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using RespawnTimer.API.Features;
using RespawnTimer.ApiFeatures;
using UserSettings.ServerSpecific;

namespace RespawnTimer
{
    public class EventHandler
    {
        // --- State ---
        private static readonly List<Player> Players = new List<Player>();
        private CoroutineHandle _timerCoroutine;

        // --- Lifecycle ---

        internal void OnWaitingForPlayers()
        {
            if (_timerCoroutine.IsRunning) 
                Timing.KillCoroutines(_timerCoroutine);
            
            Players.Clear();
        }

        internal void OnRoundStarted()
        {
            try
            {
                if (_timerCoroutine.IsRunning)
                    Timing.KillCoroutines(_timerCoroutine);

                _timerCoroutine = Timing.RunCoroutine(TimerCoroutine());
            }
            catch (Exception e)
            {
                LogManager.Error(e.ToString());
            }
        }

        // --- Player Events ---

        internal static void OnVerified(VerifiedEventArgs ev)
        {
            if (ev?.Player?.ReferenceHub == null) return;
            ServerSpecificSettingsSync.SendToPlayer(ev.Player.ReferenceHub);
        }

        internal static void OnLeft(LeftEventArgs ev)
        {
            if (ev?.Player == null) return;
            if (Players.Contains(ev.Player)) 
                Players.Remove(ev.Player);
        }

        internal static void OnRoleChanging(ChangingRoleEventArgs ev)
        {
            if (ev?.Player == null) return;
            
            RespawnTimer.Singleton.OnReloaded();
            RefreshHint(ev.Player, ev.NewRole);
        }

        // --- Server Events ---

        internal static void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
        }

        // --- Core Logic ---

        private static IEnumerator<float> TimerCoroutine()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(1f);

                if (Round.IsEnded) 
                    break;

                foreach (var player in Players)
                {
                    if (player == null) continue;

                    if (TimerView.TryGetTimerForPlayer(player, out var timerView))
                        player.ShowHint(timerView.GetText(), 1.25f);
                }
            }
        }

        internal static void RefreshHint(Player player, RoleTypeId newRole)
        {
            if (player?.ReferenceHub == null) return;

            bool isSpec = newRole == RoleTypeId.Spectator || newRole == RoleTypeId.Overwatch;
            var setting = ServerSpecificSettingsSync.GetSettingOfUser<SSTwoButtonsSetting>(player.ReferenceHub, 1);
            bool hideTimer = setting != null && setting.SyncIsB;

            if (!Round.InProgress || !isSpec || hideTimer)
            {
                Players.Remove(player);
                return;
            }

            if (!Players.Contains(player)) 
                Players.Add(player);
        }

        // --- Helpers ---

        internal static IEnumerable<Player> GetReadyPlayers() => Player.List;

        internal static RoleTypeId GetPlayerRole(Player player) => player?.Role.Type ?? RoleTypeId.None;
    }
}
