namespace TextChat
{
    public class Translation
    {
        public string Prefix { get; set; } = "<color=green>💬: </color>";

        public string Successful { get; set; } = "Mensaje enviado correctamente.";

        public string CurrentMessage { get; set; } = "<color=green>Mensaje enviado:</color>\n{0}";

        public string ContentTooLong { get; set; } = "El contenido del mensaje es demasiado largo.";

        public string ContainsBadWord { get; set; } = "Tu mensaje contiene palabras bloqueadas por el servidor.";

        public string NotValidRole { get; set; } = "No tienes un rol válido para enviar mensajes.";

        public string NoContent { get; set; } = "No puedes enviar un mensaje vacío.";
    }
}