using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;

namespace RespawnTimer.Configs;

public sealed class Config
    : IConfig
{
    public bool IsEnabled { get; set; } = true;
    [Description("Whether to enable debug messages in the console.")]
    public bool Debug { get; set; } = false;

    public Dictionary<string, string> Timers { get; set; } = new()
    {
        {
            "default", "DefaultTimer"
        }
    };
}