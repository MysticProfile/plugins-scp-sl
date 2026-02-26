#if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
using PlayerEvents = Exiled.Events.Handlers.Player;
#else
using LabApi.Loader.Features.Plugins;
#endif
using AntiSpawnKill.Configuration;
using AntiSpawnKill.Properites;
using System;

namespace AntiSpawnKill
{
    public class Plugin : Plugin<Config>
    {
        public static Plugin Instance { get; private set; }

        public override string Name => AssemblyInfo.Name;
        public override string Author => AssemblyInfo.Author;

#if EXILED
        public override Version RequiredExiledVersion => new(8, 9, 11);
        public override PluginPriority Priority => PluginPriority.First;
#else
        public override Version RequiredApiVersion => new(1, 1, 5);
        public override string Description => AssemblyInfo.Description;
#endif

        public override Version Version => new(AssemblyInfo.Version);

        private EventsHandler eventsHandler;

#if EXILED
        public override void OnEnabled()
#else
        public override void Enable()
#endif
        {
            Instance = this;
            RegisterEvents();
#if EXILED
            base.OnEnabled();
#endif
        }

#if EXILED
        public override void OnDisabled()
#else
        public override void Disable()
#endif
        {
            UnregisterEvents();
            Instance = null;
#if EXILED
            base.OnDisabled();
#endif
        }

        private void RegisterEvents()
        {
            if (Config is null || !Config.IsEnabled)
                return;

            eventsHandler = new EventsHandler();
#if EXILED
            PlayerEvents.Spawned += eventsHandler.OnSpawned;
            PlayerEvents.Hurting += eventsHandler.OnHurting;
            PlayerEvents.Left += eventsHandler.OnLeft;
#else
            eventsHandler.RegisterLabApiEvents();
#endif
        }

        private void UnregisterEvents()
        {
            if (eventsHandler is null)
                return;
#if EXILED
            PlayerEvents.Spawned -= eventsHandler.OnSpawned;
            PlayerEvents.Hurting -= eventsHandler.OnHurting;
            PlayerEvents.Left -= eventsHandler.OnLeft;
#else
            eventsHandler.UnregisterLabApiEvents();
#endif
            eventsHandler = null;
        }
    }
}
