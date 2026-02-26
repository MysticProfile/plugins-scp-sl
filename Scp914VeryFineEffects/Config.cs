using Exiled.API.Interfaces;
using System.ComponentModel;

namespace Scp914VeryFineEffects
{
    public sealed class Config : IConfig
    {
        [Description("Is the plugin enabled")]
        public bool IsEnabled { get; set; } = true;

        [Description("Enable debug logs")]
        public bool Debug { get; set; } = false;

        [Description("Duration in seconds for both effects")]
        public float DurationSeconds { get; set; } = 60f;

        [Description("Add duration if effect is already active")]
        public bool AddDurationIfActive { get; set; } = true;
    }
}
