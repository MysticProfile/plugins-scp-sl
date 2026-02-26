using Exiled.API.Interfaces;
using System.ComponentModel;

namespace RagdollCleaner
{
    public class Config : IConfig
    {
        [Description("Indica si el plugin está activado.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Indica si se deben mostrar mensajes de depuración.")]
        public bool Debug { get; set; } = false;

        [Description("Tiempo en segundos antes de que un cuerpo de SCP sea eliminado (5 minutos = 300 segundos).")]
        public float ScpBodyDuration { get; set; } = 300f;

        [Description("Tiempo en segundos antes de que un cuerpo humano sea eliminado (5 minutos = 300 segundos).")]
        public float HumanBodyDuration { get; set; } = 300f;

        [Description("Cantidad de cuerpos totales (Humanos + SCPs) que debe haber en el servidor para que se empiecen a borrar.")]
        public int BodyThreshold { get; set; } = 15;

        [Description("Desplazamiento vertical del mensaje (debe ser menor que el de ScpContainAnnouncer para estar debajo).")]
        public float HudVOffsetEm { get; set; } = 40f;

        [Description("Duración del mensaje en pantalla (segundos).")]
        public float MsgDuration { get; set; } = 3f;
    }
}
