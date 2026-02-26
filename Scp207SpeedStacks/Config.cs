using System.ComponentModel;

using Exiled.API.Interfaces;

namespace Scp207SpeedStacks
{
    public sealed class Config : IConfig
    {
        [Description("Is the plugin enabled")]
        public bool IsEnabled { get; set; } = true;

        [Description("Enable debug logs")]
        public bool Debug { get; set; } = false;

        [Description("Extra speed bonus per cola after the first one (e.g. 0.10 = +10% per extra cola)")]
        public float SpeedPerExtraCola { get; set; } = 0.10f;

        [Description("HUD refresh interval (seconds)")]
        public float HudInterval { get; set; } = 1.0f;
    }
}
