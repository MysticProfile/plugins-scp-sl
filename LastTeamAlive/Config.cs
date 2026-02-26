using System.ComponentModel;

#if EXILED
using Exiled.API.Interfaces;
#endif

namespace LastTeamAlive
{
#if EXILED
    public sealed class Config : IConfig
#else
    public sealed class Config
#endif
    {
        [Description("Is the plugin enabled")]
        public bool IsEnabled { get; set; } = true;

        [Description("Enable debug logs")]
        public bool Debug { get; set; } = false;

        [Description("HUD update interval (seconds).")]
        public float HudIntervalSeconds { get; set; } = 0.5f;

        [Description("HUD vertical offset in em. Negative moves text towards bottom.")]
        public float HudVOffsetEm { get; set; } = 0f;

        [Description("Text size percent (e.g. 70 = 70%)")]
        public int HudSizePercent { get; set; } = 70;

        [Description("Seconds to keep the warning visible after becoming last alive.")]
        public float DisplaySeconds { get; set; } = 6f;
    }
}
