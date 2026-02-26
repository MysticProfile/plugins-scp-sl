#if EXILED
using Exiled.API.Interfaces;
#endif
using System.ComponentModel;

namespace RoleEscapeHandler
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
    }
}
