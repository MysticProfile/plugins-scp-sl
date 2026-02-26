using Exiled.API.Features;
using System;

namespace Scp207SpeedStacks
{
    public sealed class Plugin : Plugin<Config, Translation>
    {
        public static Plugin Instance { get; private set; }

        public override string Name => "Scp207SpeedStacks";
        public override string Author => "Mystic";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredExiledVersion => new Version(9, 13, 1);

        private EventHandlers _handlers;

        public override void OnEnabled()
        {
            Instance = this;
            _handlers = new EventHandlers();

            _handlers.Register();

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            _handlers?.Unregister();
            _handlers = null;
            Instance = null;

            base.OnDisabled();
        }
    }
}
