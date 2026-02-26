using Exiled.API.Interfaces;

namespace RagdollCleaner
{
    public class Translation : ITranslation
    {
        public string CleanupMessage { get; set; } = "Los cuerpos se limpiarán en {duration}";
    }
}
