using Exiled.API.Features;
using System;

namespace AntiAFK
{
    public class Plugin : Plugin<Config, Translation>
    {
        public static Plugin Instance { get; private set; }

        public override string Name => "AntiAFK";
        public override string Author => "Mystic";
        public override Version Version => new Version(1, 0, 1);
        public override Version RequiredExiledVersion => new Version(9, 13, 1);

        private EventHandlers _eventHandlers;

        public override void OnEnabled()
        {
            Instance = this;
            _eventHandlers = new EventHandlers();

            _eventHandlers.Register();

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
