using Exiled.API.Enums;
using Exiled.API.Features;
using System;

namespace RoleEscapeHandler
{
    public sealed class Plugin : Plugin<Config>
    {
        public static Plugin Instance { get; private set; }

        public override string Name => "RoleEscapeHandler";
        public override string Author => "Mystic";
        public override PluginPriority Priority => PluginPriority.Default;
        public override Version Version { get; } = new Version(1, 0, 0);
        public override Version RequiredExiledVersion { get; } = new Version(9, 13, 1);

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
