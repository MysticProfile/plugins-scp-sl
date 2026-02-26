#if EXILED
using Exiled.API.Interfaces;
#endif
using System.ComponentModel;

namespace AntiSpawnKill.Configuration
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

        [Description("Immunity duration in seconds after a player spawns")]
        public float ImmunitySeconds { get; set; } = 5f;

        [Description("If true, SCPs can still damage a player during spawn immunity")]
        public bool AllowScpDamageDuringImmunity { get; set; } = false;
    }
}
