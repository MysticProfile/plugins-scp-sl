#if EXILED
using Exiled.API.Interfaces;
#endif
using System.ComponentModel;

namespace TutorialNoScpEffect
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

        [Description("Block Tutorial from triggering SCP-096 by looking at it")]
        public bool Block096LookTrigger { get; set; } = true;

        [Description("Prevent Tutorial from counting as an observer for SCP-173 (so it doesn't freeze 173)")]
        public bool Block173Observe { get; set; } = true;
    }
}
