using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using System;

namespace RoleEscapeHandler
{
    internal static class EventHandlers
    {
        // --- State ---
        private static Config Config => Plugin.Instance.Config;

        // --- Registration ---

        public static void Register()
        {
            Exiled.Events.Handlers.Player.Escaping += OnEscaping;
        }

        public static void Unregister()
        {
            Exiled.Events.Handlers.Player.Escaping -= OnEscaping;
        }

        // --- Events ---

        private static void OnEscaping(EscapingEventArgs ev)
        {
            try
            {
                if (Plugin.Instance?.Config == null || !Plugin.Instance.Config.IsEnabled || ev?.Player == null)
                    return;

                bool isGuard = ev.Player.Role.Type == RoleTypeId.FacilityGuard;
                bool isScientist = ev.Player.Role.Type == RoleTypeId.Scientist;
                bool isClassD = ev.Player.Role.Type == RoleTypeId.ClassD;
                if (!isGuard && !isScientist && !isClassD)
                    return;

                RoleTypeId newRole = isClassD ? RoleTypeId.ChaosRepressor : RoleTypeId.NtfSpecialist;
                if (isGuard)
                {
                    try
                    {
                        if (ev.Player.IsCuffed && ev.Player.Cuffer != null && IsChaos(ev.Player.Cuffer.Role.Type))
                            newRole = RoleTypeId.ChaosRepressor;
                    }
                    catch
                    {

                    }
                }

                ev.IsAllowed = true;
                ev.NewRole = newRole;

                if (Config.Debug)
                    Log.Debug($"[RoleEscapeHandler] Converted escaping {ev.Player.Role.Type} {ev.Player.Nickname} -> {ev.NewRole}");
            }
            catch (Exception e)
            {
                if (Config.Debug)
                    Log.Error($"[RoleEscapeHandler] Exception in OnEscaping: {e}");
            }
        }

        // --- Logic Helpers ---

        private static bool IsChaos(RoleTypeId role) =>
            role is RoleTypeId.ChaosConscript or RoleTypeId.ChaosRifleman or RoleTypeId.ChaosMarauder or RoleTypeId.ChaosRepressor;
    }
}
