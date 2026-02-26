#if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
using System;
#else
using System;
#endif

namespace TeamShotsAndKillsPlugin
{
#if EXILED
    public sealed class TeamShotsAndKills : Plugin<Config, Translation>
    {
        public static TeamShotsAndKills Instance { get; private set; }

        public override string Name => "TeamShotsAndKills";
        public override string Author => "Mystic";
        public override PluginPriority Priority => PluginPriority.Last;
        public override Version Version { get; } = new Version(1, 0, 0);

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
#else
    public sealed class TeamShotsAndKills : LabApi.Loader.Features.Plugins.Plugin
    {
        public static TeamShotsAndKills Instance { get; private set; }
        public Config Config { get; } = new Config();

        public override string Name { get; } = "TeamShotsAndKills";
        public override string Description { get; } = "";
        public override string Author { get; } = "Mystic";
        public override Version Version { get; } = new Version(1, 0, 0);
        public override Version RequiredApiVersion { get; } = new Version(1, 1, 5);

        public override void Enable()
        {
            Instance = this;
            EventHandlers.Register();
        }

        public override void Disable()
        {
            EventHandlers.Unregister();
            Instance = null;
        }
    }
#endif
}
