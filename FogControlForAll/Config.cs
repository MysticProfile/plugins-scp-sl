using Exiled.API.Interfaces;
using System.ComponentModel;

namespace FogControlForAll
{
    public class Config : IConfig
    {
        [Description("Indica si el plugin está activado.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Indica si se deben mostrar mensajes de depuración.")]
        public bool Debug { get; set; } = false;

        [Description("Intensidad del efecto FogControl (1 = tu intensidad default).")]
        public byte FogIntensity { get; set; } = 1;
    }
}
