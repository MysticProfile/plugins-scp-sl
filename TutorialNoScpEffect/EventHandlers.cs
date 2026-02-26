#if EXILED
using Exiled.API.Features;
using Exiled.Events.EventArgs.Scp096;
using Exiled.Events.EventArgs.Scp173;
using Scp096Events = Exiled.Events.Handlers.Scp096;
using Scp173Events = Exiled.Events.Handlers.Scp173;
#endif
using PlayerRoles;
using System;

namespace TutorialNoScpEffect
{
    internal static class EventHandlers
    {
        // --- State ---
        private static Config Config => Plugin.Instance.Config;

        // --- Registration ---

        public static void Register()
        {
#if EXILED
            Scp096Events.AddingTarget += OnScp096AddingTarget;
            Scp173Events.AddingObserver += OnScp173BeingObserved;
#endif
        }

        public static void Unregister()
        {
#if EXILED
            Scp096Events.AddingTarget -= OnScp096AddingTarget;
            Scp173Events.AddingObserver -= OnScp173BeingObserved;
#endif
        }

        // --- Events ---

#if EXILED
        private static void OnScp096AddingTarget(AddingTargetEventArgs ev)
        {
            try
            {
                if (!Config.IsEnabled || !Config.Block096LookTrigger || ev?.Target is null)
                    return;

                if (ev.Target.Role.Type != RoleTypeId.Tutorial)
                    return;

                ev.IsAllowed = false;

                if (Config.Debug)
                    Log.Debug($"[TutorialNoScpEffect] Blocked SCP-096 target add from Tutorial: {ev.Target.Nickname}");
            }
            catch (Exception e)
            {
                if (Config.Debug)
                    Log.Error($"[TutorialNoScpEffect] Exception in OnScp096AddingTarget: {e}");
            }
        }

        private static void OnScp173BeingObserved(AddingObserverEventArgs ev)
        {
            try
            {
                if (!Config.IsEnabled || !Config.Block173Observe || ev?.Observer is null)
                    return;

                if (ev.Observer.Role.Type != RoleTypeId.Tutorial)
                    return;

                ev.IsAllowed = false;

                if (Config.Debug)
                    Log.Debug($"[TutorialNoScpEffect] Blocked SCP-173 observer add from Tutorial: {ev.Observer.Nickname}");
            }
            catch (Exception e)
            {
                if (Config.Debug)
                    Log.Error($"[TutorialNoScpEffect] Exception in OnScp173AddingObserver: {e}");
            }
        }
#endif
    }
}
