#if EXILED
using Exiled.API.Interfaces;
#endif

namespace LastTeamAlive
{
#if EXILED
    public sealed class Translation : ITranslation
#else
    public sealed class Translation
#endif
    {
        public string LastAliveInTeam { get; set; } = "<color=red>Eres el único de tu equipo con vida.</color>";
    }
}
