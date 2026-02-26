using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastTeamAlive
{
    internal sealed class EventHandlers : IDisposable
    {
        // --- State ---
        private readonly Dictionary<int, CoroutineHandle> _displayCoroutines = new Dictionary<int, CoroutineHandle>();
        private readonly HashSet<string> _alreadyWarnedKeys = new HashSet<string>();
        private readonly HashSet<string> _teamHadMultipleAlive = new HashSet<string>();
        private bool _roundActive;

        private Config Config => Plugin.Instance?.Config;
        private Translation Translation => Plugin.Instance?.Translation;

        // --- Registration ---

        public void Register()
        {
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.Escaping += OnEscaping;
            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
            Exiled.Events.Handlers.Player.Left += OnLeft;
        }

        public void Unregister()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.Escaping -= OnEscaping;
            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
        }

        // --- Lifecycle ---

        public void OnWaitingForPlayers()
        {
            StopAll();
            _roundActive = false;
        }

        public void OnRoundStarted()
        {
            _roundActive = true;
            _alreadyWarnedKeys.Clear();
            _teamHadMultipleAlive.Clear();
        }

        public void OnRoundEnded(RoundEndedEventArgs ev)
        {
            _roundActive = false;
            StopAll();
        }

        public void Dispose()
        {
            StopAll();
        }

        // --- Player Events ---

        public void OnDied(DiedEventArgs ev)
        {
            if (ev?.Player is null) return;
            StopFor(ev.Player);
            
            // Usamos el equipo actual del jugador antes de que el servidor lo procese como muerto
            // o dependemos del rol para obtener el equipo.
            Team oldTeam = ev.TargetOldRole.GetTeam();
            Timing.CallDelayed(0.2f, () => CheckTeamsForVictim(ev.Player, oldTeam, ev.TargetOldRole));
        }

        public void OnLeft(LeftEventArgs ev)
        {
            if (ev?.Player is null) return;
            StopFor(ev.Player);
            Timing.CallDelayed(0.2f, () => CheckTeamsForVictim(ev.Player, ev.Player.Role.Team, ev.Player.Role.Type));
        }

        public void OnEscaping(EscapingEventArgs ev)
        {
            if (ev?.Player is null || !ev.IsAllowed) return;
            Timing.CallDelayed(0.2f, () => CheckTeamsForVictim(ev.Player, ev.Player.Role.Team, ev.Player.Role.Type));
        }

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev?.Player is null) return;
            StopFor(ev.Player);
            
            if (_roundActive && ev.Player.IsAlive)
            {
                Timing.CallDelayed(0.2f, () => CheckTeamsForVictim(ev.Player, ev.Player.Role.Team, ev.Player.Role.Type));
            }
        }

        // --- Team Logic ---

        private void CheckTeamsForVictim(Player victim, Team oldTeam, RoleTypeId oldRole)
        {
            if (!_roundActive || Config is null || !Config.IsEnabled || victim is null) 
                return;

            if (oldRole == RoleTypeId.Tutorial)
                return;

            if (oldRole == RoleTypeId.FacilityGuard)
            {
                CheckTeam(Team.FoundationForces, RoleTypeId.FacilityGuard);
                return;
            }

            CheckTeam(oldTeam, RoleTypeId.None);
        }

        private void CheckTeam(Team team, RoleTypeId specificRole)
        {
            List<Player> alive = new List<Player>();

            foreach (var p in Player.List)
            {
                if (p is null || !p.IsAlive || p.IsNPC || p.Role.Type == RoleTypeId.Tutorial)
                    continue;

                if (specificRole != RoleTypeId.None)
                {
                    if (p.Role.Type == specificRole)
                        alive.Add(p);
                }
                else if (p.Role.Team == team)
                {
                    alive.Add(p);
                }
            }

            string teamKey = (specificRole != RoleTypeId.None) ? $"{team}|{specificRole}" : team.ToString();
            
            if (alive.Count >= 2)
                _teamHadMultipleAlive.Add(teamKey);

            if (alive.Count != 1 || !_teamHadMultipleAlive.Contains(teamKey))
                return;

            Player last = alive[0];
            if (last is null || !last.IsConnected || last.IsNPC)
                return;

            string key = BuildKey(last);
            if (_alreadyWarnedKeys.Contains(key))
                return;

            _alreadyWarnedKeys.Add(key);
            StartDisplay(last);
        }

        // --- UI / Display ---

        private void StartDisplay(Player player)
        {
            if (player is null) return;

            StopFor(player);
            _displayCoroutines[player.Id] = Timing.RunCoroutine(DisplayOnce(player));
        }

        private IEnumerator<float> DisplayOnce(Player player)
        {
            if (_roundActive && Config != null && Config.IsEnabled && player != null && player.IsConnected && player.IsAlive)
            {
                string msg = Translation?.LastAliveInTeam;
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    float hintDuration = Mathf.Max(0.2f, Config.DisplaySeconds);
                    player.ShowHint(msg, hintDuration);
                }
            }

            yield return Timing.WaitForSeconds(0.05f);

            if (player != null)
                _displayCoroutines.Remove(player.Id);
        }

        // --- Helpers ---

        private string BuildKey(Player player)
        {
            if (player == null) return string.Empty;
            return $"{player.UserId}|{player.Role.Team}|{player.Role.Type}";
        }

        private void StopAll()
        {
            foreach (var h in _displayCoroutines.Values)
                if (h.IsRunning) Timing.KillCoroutines(h);

            _displayCoroutines.Clear();
            _alreadyWarnedKeys.Clear();
            _teamHadMultipleAlive.Clear();
        }

        private void StopFor(Player player)
        {
            if (player is null) return;

            if (_displayCoroutines.TryGetValue(player.Id, out var h) && h.IsRunning)
                Timing.KillCoroutines(h);

            _displayCoroutines.Remove(player.Id);
        }
    }
}
