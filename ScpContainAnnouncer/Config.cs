using System.ComponentModel;

#if EXILED
using Exiled.API.Interfaces;
#endif

namespace ScpContainAnnouncer
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

        [Description("How long the message stays on screen (seconds)")]
        public float HintSeconds { get; set; } = 4f;

        [Description("HUD update interval (seconds). Lower = more stable but can overwrite other hints")]
        public float HudIntervalSeconds { get; set; } = 0.25f;

        [Description("HUD vertical offset in em.")]
        public float HudVOffsetEm { get; set; } = 50f;
    }
}
