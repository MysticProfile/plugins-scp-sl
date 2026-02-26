#if EXILED
using Exiled.API.Interfaces;
#endif

namespace ScpContainAnnouncer
{
#if EXILED
    public sealed class Translation : ITranslation
#else
    public sealed class Translation
#endif
    {
        public string Message { get; set; } = "{player} ha contenido al {scp}";
    }
}
