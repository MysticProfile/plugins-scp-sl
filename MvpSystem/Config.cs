using System.ComponentModel;

#if EXILED
using Exiled.API.Interfaces;
#endif

namespace MvpSystem;

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

    [Description("Count teamkills in stats (default: false)")]
    public bool CountTeamKills { get; set; } = false;
}