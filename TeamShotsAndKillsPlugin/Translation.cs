#if EXILED
using Exiled.API.Interfaces;

namespace TeamShotsAndKillsPlugin
{
    public sealed class Translation : ITranslation
    {
        public string TeamDamageAttackerMessage { get; set; } = "Has hecho daño a un compañero: {target}";
        public string TeamDamageTargetMessage { get; set; } = "Te ha hecho daño tu compañero: {attacker}";

        public string TeamKillAttackerMessage { get; set; } = "<color=red>Has matado a un compañero: {target}</color>";

        public string KillMessage { get; set; } = "💀 Has matado a {target}";
    }
}
#endif
