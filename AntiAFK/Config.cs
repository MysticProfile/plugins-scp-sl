using Exiled.API.Interfaces;
using System.ComponentModel;

namespace AntiAFK
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        [Description("Tiempo en segundos para considerar a un jugador AFK (3 minutos = 180s)")]
        public float AfkTime { get; set; } = 180f;

        [Description("Tiempo en segundos antes de ser mandado a espectador para mostrar el aviso (1 minuto = 60s)")]
        public float WarningTime { get; set; } = 60f;

        [Description("Intervalo de actualización del HUD en segundos.")]
        public float HudInterval { get; set; } = 0.5f;
    }
}
