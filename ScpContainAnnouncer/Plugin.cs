using Exiled.API.Enums;
using Exiled.API.Features;
using System;

namespace ScpContainAnnouncer
{
    public sealed class Plugin : Plugin<Config, Translation>
    {
        public static Plugin Instance { get; private set; }

        public override string Name => "ScpContainAnnouncer";
        public override string Author => "Mystic";
        public override PluginPriority Priority => PluginPriority.Last;
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredExiledVersion => new Version(9, 13, 1);

        public override void OnEnabled()
        {
            Instance = this;
            EventHandlers.Register();
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            EventHandlers.Unregister();
            Instance = null;
            base.OnDisabled();
        }
    }
}
