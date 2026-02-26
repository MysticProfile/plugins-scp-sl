using Exiled.API.Enums;
using Exiled.API.Features;
using System;

namespace LastTeamAlive
{
    public sealed class Plugin : Plugin<Config, Translation>
    {
        public static Plugin Instance { get; private set; }

        public override string Name => "LastTeamAlive";
        public override string Author => "Mystic";
        public override PluginPriority Priority => PluginPriority.Last;
        public override Version Version { get; } = new Version(1, 0, 0);
        public override Version RequiredExiledVersion { get; } = new Version(9, 13, 1);

        private EventHandlers _handlers;

        public override void OnEnabled()
        {
            Instance = this;

            if (Config != null && Config.IsEnabled)
            {
                _handlers = new EventHandlers();
                _handlers.Register();
            }

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            if (_handlers != null)
            {
                _handlers.Unregister();
                _handlers.Dispose();
                _handlers = null;
            }

            Instance = null;
            base.OnDisabled();
        }
    }
}
