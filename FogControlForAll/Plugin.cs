using Exiled.API.Enums;
using Exiled.API.Features;
using System;

namespace FogControlForAll
{
    public class Plugin : Plugin<Config>
    {
        public static Plugin Instance { get; private set; }

        public override string Name => "FogControlForAll";
        public override string Author => "Mystic";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredExiledVersion => new Version(9, 13, 1);
        public override PluginPriority Priority => PluginPriority.Default;

        private EventHandlers _eventHandlers;

        public override void OnEnabled()
        {
            Instance = this;

            if (Config != null && Config.IsEnabled)
            {
                _eventHandlers = new EventHandlers(Config);
                _eventHandlers.Register();
            }

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            _eventHandlers?.Unregister();
            _eventHandlers = null;
            Instance = null;

            base.OnDisabled();
        }
    }
}
