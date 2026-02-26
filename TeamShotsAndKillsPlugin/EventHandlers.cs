#if EXILED

using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;

#else

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;

#endif

using PlayerRoles;
using System;
using System.Collections.Generic;

namespace TeamShotsAndKillsPlugin

{

    internal static class EventHandlers

    {
        private static readonly Dictionary<string, float> LastTeamDamageMessageAt = new();

        private static readonly Dictionary<string, float> LastTeamKillMessageAt = new();

        private static readonly Dictionary<string, float> LastLethalTeamHitAt = new();

#if EXILED

        private enum HintKind
        {
            TeamDamage,
            TeamKill,
        }

#endif

        private static readonly DateTime UnixEpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static float NowSeconds => (float)DateTime.UtcNow.Subtract(UnixEpochUtc).TotalSeconds;

        internal static void Register()
        {

#if EXILED

            Exiled.Events.Handlers.Player.Hurting += OnHurting;

            Exiled.Events.Handlers.Player.Dying += OnDying;

#else

            PlayerEvents.Hurting += OnPlayerHurting;

            PlayerEvents.Dying += OnPlayerDying;

#endif

        }

        internal static void Unregister()
        {

#if EXILED

            Exiled.Events.Handlers.Player.Hurting -= OnHurting;

            Exiled.Events.Handlers.Player.Dying -= OnDying;

#else

            PlayerEvents.Hurting -= OnPlayerHurting;

            PlayerEvents.Dying -= OnPlayerDying;

#endif

            LastTeamDamageMessageAt.Clear();

            LastTeamKillMessageAt.Clear();

            LastLethalTeamHitAt.Clear();

        }

        private static string PairKey(string attackerId, string targetId) => $"{attackerId}->{targetId}";

#if EXILED

        private static void ShowHintSafe(Player player, string message, float seconds)
        {
            if (player is null)

                return;

            if (string.IsNullOrEmpty(message))

                return;

            if (seconds <= 0f)

                return;

            player.ShowHint(message, seconds);
        }

        private static void ClearHint(Player player)
        {
            if (player is null)

                return;

            player.ShowHint(" ", 0.1f);
        }

        private static string ColorizeName(Player player)
        {
            if (player is null)

                return string.Empty;

            string name = player.Nickname ?? string.Empty;

            RoleTypeId role = player.Role?.Type ?? RoleTypeId.None;

            Team team = player.Role?.Team ?? Team.OtherAlive;

            string color = null;

            if (role == RoleTypeId.Scp0492)

                color = "red";

            if (role == RoleTypeId.ClassD)

                color = "orange";

            else if (team == Team.ChaosInsurgency)

                color = "green";

            else if (team == Team.FoundationForces)

                color = "blue";

            else if (role == RoleTypeId.Scientist)

                color = "yellow";

            if (string.IsNullOrEmpty(color))

                return name;

            return $"<color={color}>{name}</color>";
        }

        private static void ShowHint(Player player, HintKind kind, string message, float durationSeconds)
        {
            if (player == null || string.IsNullOrEmpty(message))

                return;

            player.ShowHint(message, durationSeconds);
        }

        private static bool IsFriendly(Player attacker, Player target)
        {

            if (attacker is null || target is null)

                return false;

            if (attacker == target)

                return false;

            if (attacker.Role is null || target.Role is null)

                return false;

            Team at = attacker.Role.Team;

            Team tt = target.Role.Team;

            if (at is Team.Dead || tt is Team.Dead)

                return false;

            if (at == tt)

                return true;

            RoleTypeId ar = attacker.Role.Type;

            RoleTypeId tr = target.Role.Type;

            bool aFoundation = at == Team.FoundationForces;

            bool tFoundation = tt == Team.FoundationForces;

            if ((aFoundation && tr == RoleTypeId.Scientist) || (tFoundation && ar == RoleTypeId.Scientist))

                return true;

            if ((ar == RoleTypeId.ClassD && tt == Team.ChaosInsurgency) || (tr == RoleTypeId.ClassD && at == Team.ChaosInsurgency))

                return true;

            return false;
        }



        private static bool CooldownOk(Dictionary<string, float> map, Player attacker, Player target, float cooldownSeconds)
        {

            if (attacker is null || target is null)

                return false;

            string attackerId = attacker.UserId ?? attacker.Id.ToString();

            string targetId = target.UserId ?? target.Id.ToString();

            string key = PairKey(attackerId, targetId);

            float now = NowSeconds;

            if (map.TryGetValue(key, out float last) && now - last < Math.Max(0f, cooldownSeconds))

                return false;

            map[key] = now;

            return true;
        }

        private static void MarkLethalTeamHit(Player attacker, Player target)
        {

            if (attacker is null || target is null)

                return;

            string attackerId = attacker.UserId ?? attacker.Id.ToString();

            string targetId = target.UserId ?? target.Id.ToString();

            string key = PairKey(attackerId, targetId);

            LastLethalTeamHitAt[key] = NowSeconds;

        }

        private static bool WasRecentlyMarkedLethalTeamHit(Player attacker, Player target, float windowSeconds)
        {
            if (attacker is null || target is null)

                return false;

            string attackerId = attacker.UserId ?? attacker.Id.ToString();

            string targetId = target.UserId ?? target.Id.ToString();

            string key = PairKey(attackerId, targetId);

            if (!LastLethalTeamHitAt.TryGetValue(key, out float t))

                return false;

            return NowSeconds - t <= Math.Max(0f, windowSeconds);
        }

        private static void OnHurting(HurtingEventArgs ev)
        {
            if (TeamShotsAndKills.Instance?.Config is null || !TeamShotsAndKills.Instance.Config.IsEnabled)

                return;

            if (ev?.Player is null || ev.Attacker is null)

                return;

            if (ev.Player.Role?.Type == RoleTypeId.Scp3114 || ev.Attacker.Role?.Type == RoleTypeId.Scp3114)

                return;

            if (TeamShotsAndKills.Instance.Config.SuppressHintsForClassD)
            {
                if (ev.Player.Role?.Type == RoleTypeId.ClassD && ev.Attacker.Role?.Type == RoleTypeId.ClassD)

                    return;
            }

            if (!IsFriendly(ev.Attacker, ev.Player))

                return;

            if (ev.Amount <= 0f)

                return;

            if (ev.Player.Health - ev.Amount <= 0f)

            {
                MarkLethalTeamHit(ev.Attacker, ev.Player);

                return;
            }

            if (!CooldownOk(LastTeamDamageMessageAt, ev.Attacker, ev.Player, TeamShotsAndKills.Instance.Config.TeamDamageCooldownSeconds))

                return;

            string attackerName = ColorizeName(ev.Attacker);

            string targetName = ColorizeName(ev.Player);

            string msgAttacker = TeamShotsAndKills.Instance.Translation.TeamDamageAttackerMessage

                .Replace("{attacker}", attackerName)

                .Replace("{target}", targetName);

            string msgTarget = TeamShotsAndKills.Instance.Translation.TeamDamageTargetMessage

                .Replace("{attacker}", attackerName)

                .Replace("{target}", targetName);

            ShowHint(ev.Attacker, HintKind.TeamDamage, msgAttacker, TeamShotsAndKills.Instance.Config.TeamDamageHintSeconds);

            ShowHint(ev.Player, HintKind.TeamDamage, msgTarget, TeamShotsAndKills.Instance.Config.TeamDamageHintSeconds);
        }

        private static void OnDying(DyingEventArgs ev)
        {

            if (TeamShotsAndKills.Instance?.Config is null || !TeamShotsAndKills.Instance.Config.IsEnabled)

                return;

            if (ev?.Player is null || ev.Attacker is null || ev.Attacker == ev.Player)

                return;

            if (ev.Player.Role?.Type == RoleTypeId.Scp3114 || ev.Attacker.Role?.Type == RoleTypeId.Scp3114)

                return;

            if (TeamShotsAndKills.Instance.Config.SuppressHintsForClassD)

            {
                if (ev.Player.Role?.Type == RoleTypeId.ClassD && ev.Attacker.Role?.Type == RoleTypeId.ClassD)

                    return;
            }

            if (!IsFriendly(ev.Attacker, ev.Player))
            {

                bool allowKillHint = ev.Attacker.IsHuman || ev.Player.Role?.Type == RoleTypeId.Scp0492;

                if (!allowKillHint)

                    return;

                string targetNameColored = ColorizeName(ev.Player);

                string killMsg = TeamShotsAndKills.Instance.Translation.KillMessage

                    .Replace("{target}", targetNameColored);

                ShowHint(ev.Attacker, HintKind.TeamKill, killMsg, 3f);

                return;

            }

            ClearHint(ev.Player);

            ClearHint(ev.Attacker);

            if (!CooldownOk(LastTeamKillMessageAt, ev.Attacker, ev.Player, TeamShotsAndKills.Instance.Config.TeamKillCooldownSeconds))

                return;

            string attackerName = ev.Attacker.Nickname;

            string targetName = ev.Player.Nickname;

            string msgAttacker = TeamShotsAndKills.Instance.Translation.TeamKillAttackerMessage

                .Replace("{attacker}", attackerName)

                .Replace("{target}", targetName);

            ShowHint(ev.Attacker, HintKind.TeamKill, msgAttacker, TeamShotsAndKills.Instance.Config.TeamKillHintSeconds);
        }

#else

        private static string ColorizeName(Player player)
        {
            if (player is null)

                return string.Empty;

            string name = player.Nickname ?? string.Empty;

            RoleTypeId role = player.Role;

            Team team = player.Team;

            string color = null;

            if (role == RoleTypeId.Scp0492)

                color = "red";

            if (role == RoleTypeId.ClassD)

                color = "orange";

            else if (team == Team.ChaosInsurgency)

                color = "green";

            else if (team == Team.FoundationForces)

                color = "blue";

            else if (role == RoleTypeId.Scientist)

                color = "yellow";

            if (string.IsNullOrEmpty(color))

                return name;

            return $"<color={color}>{name}</color>";
        }

        private static bool IsFriendly(Player attacker, Player target)
        {
            if (attacker is null || target is null)

                return false;

            if (attacker == target)

                return false;

            Team at = attacker.Team;

            Team tt = target.Team;

            if (at is Team.Dead || tt is Team.Dead)

                return false;

            if (at == tt)

                return true;

            RoleTypeId ar = attacker.Role;

            RoleTypeId tr = target.Role;

            bool aFoundation = at == Team.FoundationForces;

            bool tFoundation = tt == Team.FoundationForces;

            if ((aFoundation && tr == RoleTypeId.Scientist) || (tFoundation && ar == RoleTypeId.Scientist))

                return true;

            if ((ar == RoleTypeId.ClassD && tt == Team.ChaosInsurgency) || (tr == RoleTypeId.ClassD && at == Team.ChaosInsurgency))

                return true;

            return false;
        }

        private static bool CooldownOk(Dictionary<string, float> map, Player attacker, Player target, float cooldownSeconds)
        {
            if (attacker is null || target is null)

                return false;

            string attackerId = attacker.UserId ?? attacker.PlayerId.ToString();

            string targetId = target.UserId ?? target.PlayerId.ToString();

            string key = PairKey(attackerId, targetId);

            float now = NowSeconds;

            if (map.TryGetValue(key, out float last) && now - last < Math.Max(0f, cooldownSeconds))

                return false;

            map[key] = now;

            return true;
        }

        private static void MarkLethalTeamHit(Player attacker, Player target)
        {
            if (attacker is null || target is null)

                return;

            string attackerId = attacker.UserId ?? attacker.PlayerId.ToString();

            string targetId = target.UserId ?? target.PlayerId.ToString();

            string key = PairKey(attackerId, targetId);

            LastLethalTeamHitAt[key] = NowSeconds;
        }

        private static bool WasRecentlyMarkedLethalTeamHit(Player attacker, Player target, float windowSeconds)
        {

            if (attacker is null || target is null)

                return false;

            string attackerId = attacker.UserId ?? attacker.PlayerId.ToString();

            string targetId = target.UserId ?? target.PlayerId.ToString();

            string key = PairKey(attackerId, targetId);

            if (!LastLethalTeamHitAt.TryGetValue(key, out float t))

                return false;

            return NowSeconds - t <= Math.Max(0f, windowSeconds);

        }

        private static void OnPlayerHurting(PlayerHurtingEventArgs ev)
        {
            if (TeamShotsAndKills.Instance?.Config is null || !TeamShotsAndKills.Instance.Config.IsEnabled)

                return;

            if (ev?.Player is null || ev.Attacker is null)

                return;

            if (ev.Player.Role == RoleTypeId.Scp3114 || ev.Attacker.Role == RoleTypeId.Scp3114)

                return;

            if (TeamShotsAndKills.Instance.Config.SuppressHintsForClassD)
            {
                if (ev.Player.Role == RoleTypeId.ClassD && ev.Attacker.Role == RoleTypeId.ClassD)

                    return;
            }

            if (!IsFriendly(ev.Attacker, ev.Player))

                return;

            if (ev.Player.Health - ev.DamageHandler.Damage <= 0f)
            {
                MarkLethalTeamHit(ev.Attacker, ev.Player);

                return;
            }

            if (!CooldownOk(LastTeamDamageMessageAt, ev.Attacker, ev.Player, TeamShotsAndKills.Instance.Config.TeamDamageCooldownSeconds))

                return;

            string attackerName = ColorizeName(ev.Attacker);

            string targetName = ColorizeName(ev.Player);

            string msgAttacker = $"Has disparado a un compañero: {targetName}";

            string msgTarget = $"Te ha disparado tu compañero: {attackerName}";

            ev.Attacker.SendHint(msgAttacker, TeamShotsAndKills.Instance.Config.TeamDamageHintSeconds);

            ev.Player.SendHint(msgTarget, TeamShotsAndKills.Instance.Config.TeamDamageHintSeconds);
        }

        private static void OnPlayerDying(PlayerDyingEventArgs ev)
        {
            if (TeamShotsAndKills.Instance?.Config is null || !TeamShotsAndKills.Instance.Config.IsEnabled)

                return;

            if (ev?.Player is null || ev.Attacker is null)

                return;

            if (ev.Player.Role == RoleTypeId.Scp3114 || ev.Attacker.Role == RoleTypeId.Scp3114)

                return;

            if (TeamShotsAndKills.Instance.Config.SuppressHintsForClassD)
            {
                if (ev.Player.Role == RoleTypeId.ClassD && ev.Attacker.Role == RoleTypeId.ClassD)

                    return;
            }

            if (!IsFriendly(ev.Attacker, ev.Player))
            {
                if (ev.Attacker.Role.GetTeam() == Team.SCPs && ev.Player.Role != PlayerRoles.RoleTypeId.Scp0492)

                    return;

                string targetNameColored = ColorizeName(ev.Player);

                string killMsg = TeamShotsAndKills.Instance.Config.KillMessage

                    .Replace("{target}", targetNameColored);

                ev.Attacker.SendHint(killMsg, 3f);

                return;
            }

            if (WasRecentlyMarkedLethalTeamHit(ev.Attacker, ev.Player, 2f))

            {
                ev.Attacker.SendHint(" ", 0.1f);

                ev.Player.SendHint(" ", 0.1f);
            }

            if (!CooldownOk(LastTeamKillMessageAt, ev.Attacker, ev.Player, TeamShotsAndKills.Instance.Config.TeamKillCooldownSeconds))

                return;

            string attackerName = ev.Attacker.Nickname;

            string targetName = ev.Player.Nickname;

            string msgAttacker = $"<color=red>Has matado a un compañero: {targetName}</color>";

            ev.Attacker.SendHint(msgAttacker, TeamShotsAndKills.Instance.Config.TeamKillHintSeconds);

        }
#endif

    }
}

