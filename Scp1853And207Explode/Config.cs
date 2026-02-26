using System.ComponentModel;
using Exiled.API.Interfaces;

namespace Scp1853And207Explode
{
    public sealed class Config : IConfig
    {
        [Description("Is the plugin enabled")]
        public bool IsEnabled { get; set; } = true;

        [Description("Enable debug logs")]
        public bool Debug { get; set; } = false;

        [Description("If true, spawns an active frag grenade at the player's position (visual explosion).")]
        public bool SpawnGrenadeExplosion { get; set; } = true;

        [Description("Grenade fuse time in seconds (0 = instant). Only used if SpawnGrenadeExplosion is true.")]
        public float GrenadeFuseTime { get; set; } = 0f;

        [Description("Grenade max radius. Only used if SpawnGrenadeExplosion is true.")]
        public float GrenadeMaxRadius { get; set; } = 6f;

        [Description("Extra damage dealt to the player to guarantee death (in addition to grenade).")]
        public float ExtraLethalDamage { get; set; } = 500f;

        [Description("Cooldown (seconds) per player to prevent double-triggering.")]
        public float CooldownSeconds { get; set; } = 1.0f;
    }
}
