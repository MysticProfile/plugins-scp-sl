#if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using PlayerEvents = Exiled.Events.Handlers.Player;
using System.Linq;
#else
using LabApi.Loader.Features.Plugins;
using LabApi.Events.Handlers;
#endif
using ShootingInteractions.Configuration;
using ShootingInteractions.Properites;
using System;
using System.Reflection;

namespace ShootingInteractions
{
    public class Plugin : Plugin<Config>
    {
        public static Plugin Instance { get; private set; }
        internal static MethodInfo GetCustomItem = null;

        public override string Name => AssemblyInfo.Name;
        public override string Author => AssemblyInfo.Author;
        public override Version Version => new Version(AssemblyInfo.Version);

#if EXILED
        public override Version RequiredExiledVersion => new Version(9, 13, 1);
        public override PluginPriority Priority => PluginPriority.First;
#else
        public override Version RequiredApiVersion => new Version(1, 1, 2);
        public override string Description => AssemblyInfo.Description;
#endif

        private EventsHandler _eventsHandler;

        public override void OnEnabled()
        {
            Instance = this;
            _eventsHandler = new EventsHandler();

            RegisterEvents();

#if EXILED
            try
            {
                Assembly customItems = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly => assembly.GetName().Name == "Exiled.CustomItems");

                if (customItems != null)
                {
                    Type customItemType = customItems.GetType("Exiled.CustomItems.API.Features.CustomItem");
                    GetCustomItem = customItemType?.GetMethod("TryGet", new[] { typeof(Pickup), customItemType.MakeByRefType() });
                }
            }
            catch (Exception e)
            {
                Log.Warn($"[ShootingInteractions] Failed to hook Exiled.CustomItems: {e.Message}");
            }

            base.OnEnabled();
#endif
        }

        public override void OnDisabled()
        {
            UnregisterEvents();
            _eventsHandler = null;
            Instance = null;

#if EXILED
            base.OnDisabled();
#endif
        }

        private void RegisterEvents()
        {
#if EXILED
            PlayerEvents.Shot += _eventsHandler.OnShot;
#else
            PlayerEvents.ShotWeapon += _eventsHandler.OnShot;
#endif
        }

        private void UnregisterEvents()
        {
#if EXILED
            PlayerEvents.Shot -= _eventsHandler.OnShot;
#else
            PlayerEvents.ShotWeapon -= _eventsHandler.OnShot;
#endif
        }
    }
}
