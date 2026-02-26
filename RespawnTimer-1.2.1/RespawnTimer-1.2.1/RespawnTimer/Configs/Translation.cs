using Exiled.API.Interfaces;
using System.ComponentModel;

namespace RespawnTimer.Configs;

public sealed class Translation : ITranslation
{
    [Description("Mensaje del enlace de Discord.")]
    public string DiscordLink { get; set; } = "dsc.gg/aistrya";

    [Description("Offset X opcional (en pixeles) para mover el contador de espectadores. 0 = centrado real.")]
    public int SpectatorsXOffset { get; set; } = 0;

    [Description("Formato para el contador de espectadores. Usa {spectators_num}.")]
    public string SpectatorsCount { get; set; } = "<align=center><color=#A0A0A0>Espectadores: {spectators_num}</color></align>";

    [Description("Formato para los contadores de spawn. Usa {ci}, {ntf}, {mci}, {mntf}.")]
    public string SpawnCounters { get; set; } = "<align=center><color=blue>NTF</color>: {ntf} | <color=blue>Mini NTF</color>: {mntf}     <color=green>Caos</color>: {ci} | <color=green>Mini Caos</color>: {mci}</align>";

    [Description("Formato para el tiempo de ronda.")]
    public string RoundTime { get; set; } = "<align=center>Tiempo de Ronda: {round_minutes}:{round_seconds}</align>";
}
