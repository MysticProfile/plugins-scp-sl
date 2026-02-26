using Exiled.API.Interfaces;
using System.ComponentModel;

namespace AntiAFK
{
    public class Translation : ITranslation
    {
        [Description("Mensaje que se muestra cuando el jugador es reemplazado.")]
        public string ReplacedMessage { get; set; } = "<color=red>Has sido movido a espectador por estar AFK.</color>";

        [Description("Mensaje que se muestra al espectador que reemplaza al jugador AFK.")]
        public string ReplacementMessage { get; set; } = "<color=green>Has reemplazado a un jugador AFK.</color>";

        [Description("Mensaje de advertencia de AFK. {time} será reemplazado por el tiempo restante.")]
        public string WarningMessage { get; set; } = "Serás enviado a espectador por estar AFK {time}";
    }
}
