using System.ComponentModel;

#if EXILED
using Exiled.API.Interfaces;
#endif

namespace TeamShotsAndKillsPlugin
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

        [Description("Hint duration (seconds) for team damage")]
        public float TeamDamageHintSeconds { get; set; } = 3f;

        [Description("Hint duration (seconds) for team kills")]
        public float TeamKillHintSeconds { get; set; } = 2f;

        [Description("Do not show team damage/kill messages to Class-D players")]
        public bool SuppressHintsForClassD { get; set; } = true;

        [Description("Cooldown (seconds) per attacker for the team damage message to avoid spam")]
        public float TeamDamageCooldownSeconds { get; set; } = 0f;

        [Description("Cooldown (seconds) per attacker for the team kill message")]
        public float TeamKillCooldownSeconds { get; set; } = 0f;

        [Description("Message shown when you kill an enemy")]
        public string KillMessage { get; set; } = "💀 Has matado a {target}";
    }
}
