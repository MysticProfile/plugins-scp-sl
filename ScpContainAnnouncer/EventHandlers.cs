#if EXILED
using Exiled.Events.EventArgs.Player;
using ExiledPlayer = Exiled.API.Features.Player;
#else
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
#endif
using PlayerRoles;
using System;

namespace ScpContainAnnouncer
{
    internal static class EventHandlers
    {
        internal static void Register()
        {
#if EXILED
            Exiled.Events.Handlers.Player.Dying += OnDying;
#else
            PlayerEvents.Dying += OnPlayerDying;
#endif
        }

        internal static void Unregister()
        {
#if EXILED
            Exiled.Events.Handlers.Player.Dying -= OnDying;
#else
            PlayerEvents.Dying -= OnPlayerDying;
#endif
        }

#if EXILED
        private static void OnDying(DyingEventArgs ev)
        {
            if (Plugin.Instance?.Config is null || !Plugin.Instance.Config.IsEnabled)
                return;

            if (ev?.Player is null)
                return;

            if (ev.Player.Role is null)
                return;

            if (!IsScpRole(ev.Player.Role.Type))
                return;

            if (ev.Attacker is null)
                return;

            if (ev.Attacker == ev.Player)
                return;

            string killerName = ColorizeName(ev.Attacker);
            string scpName = ColorizeScp(ev.Player.Role.Type);

            string msg = Plugin.Instance.Translation.Message
                .Replace("{player}", killerName)
                .Replace("{scp}", scpName);

            ShowHudToAll(msg, Plugin.Instance.Config.HintSeconds);
        }

        private static bool IsScpRole(RoleTypeId role)
        {
            return role == RoleTypeId.Scp049 ||
                role == RoleTypeId.Scp079 ||
                role == RoleTypeId.Scp096 ||
                role == RoleTypeId.Scp106 ||
                role == RoleTypeId.Scp173 ||
                role == RoleTypeId.Scp939 ||
                role == RoleTypeId.Scp3114;
        }

        private static string ColorizeName(ExiledPlayer player)
        {
            if (player is null)
                return string.Empty;

            string name = player.Nickname ?? string.Empty;
            RoleTypeId role = player.Role?.Type ?? RoleTypeId.None;
            Team team = player.Role?.Team ?? Team.OtherAlive;

            string color = null;

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

        private static string ColorizeScp(RoleTypeId scpRole)
        {
            string name = scpRole.ToString();
            return $"<color=red>{name}</color>";
        }

        private static void ShowHudToAll(string msg, float seconds)
        {
            if (string.IsNullOrWhiteSpace(msg))
                return;

            float voffset = Plugin.Instance?.Config is null ? 50f : Plugin.Instance.Config.HudVOffsetEm;
            string styled = $"<align=right><size=70%><line-height=85%><voffset={voffset}em>{msg}</voffset></line-height></size></align>";
            float duration = Math.Max(0.2f, seconds);

            foreach (ExiledPlayer p in ExiledPlayer.List)
                p.ShowHint(styled, duration);
        }
#else
        private static void OnPlayerDying(PlayerDyingEventArgs ev)
        {
            if (Plugin.Instance?.Config is null || !Plugin.Instance.Config.IsEnabled)
                return;

            if (ev?.Player is null)
                return;

            if (!IsScpRole(ev.Player.Role))
                return;

            if (ev.Attacker is null)
                return;

            if (ev.Attacker == ev.Player)
                return;

            string killerName = ColorizeName(ev.Attacker);
            string scpName = ColorizeScp(ev.Player.Role);

            string msg = Plugin.Instance.Translation.Message
                .Replace("{player}", killerName)
                .Replace("{scp}", scpName);

            ShowHudToAll(msg, Plugin.Instance.Config.HintSeconds);
        }

        private static bool IsScpRole(RoleTypeId role)
        {
            return role == RoleTypeId.Scp049 ||
                role == RoleTypeId.Scp079 ||
                role == RoleTypeId.Scp096 ||
                role == RoleTypeId.Scp106 ||
                role == RoleTypeId.Scp173 ||
                role == RoleTypeId.Scp939 ||
                role == RoleTypeId.Scp3114;
        }

        private static string ColorizeName(Player player)
        {
            if (player is null)
                return string.Empty;

            string name = player.Nickname ?? string.Empty;
            RoleTypeId role = player.Role;
            Team team = player.Team;

            string color = null;

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

        private static string ColorizeScp(RoleTypeId scpRole)
        {
            string name = scpRole.ToString();
            return $"<color=red>{name}</color>";
        }

        private static void ShowHudToAll(string msg, float seconds)
        {
            if (string.IsNullOrWhiteSpace(msg))
                return;

            float voffset = Plugin.Instance?.Config is null ? 50f : Plugin.Instance.Config.HudVOffsetEm;
            string styled = $"<align=right><size=70%><line-height=85%><voffset={voffset}em>{msg}</voffset></line-height></size></align>";
            float duration = Math.Max(0.2f, seconds);

            foreach (Player p in Player.List)
                p.SendHint(styled, duration);
        }
#endif
    }
}
