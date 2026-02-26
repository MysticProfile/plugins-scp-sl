 #if EXILED
 using Exiled.API.Interfaces;
 #endif

 namespace MvpSystem;

 public sealed class Translation
 #if EXILED
     : ITranslation
 #endif
 {
    public string Header { get; set; } = "Resultados de la ronda:";

    public string MostKillsAsRole { get; set; } = "Más kills como {role}: {name} | {kills} kills";
    public string MostScpsKilled { get; set; } = "Más <color=red>SCPs</color> eliminados: {name} | {count} <color=red>SCPs</color>";
    public string ScpWithMostKills { get; set; } = "<color=red>SCP</color> con más kills: {scp} ({name}) | {kills} kills";
    public string MostScpItems { get; set; } = "Más objetos <color=red>SCP</color>: {name} | {count} objetos";
    public string FirstEscape { get; set; } = "Primero en escapar como {role}: {name}";

    public string NoKillsAsRole { get; set; } = "No hubo kills en esta ronda";
    public string NoScpsKilled { get; set; } = "No se eliminaron <color=red>SCPs</color>";
    public string NoScpWithMostKills { get; set; } = "Ningún <color=red>SCP</color> registró kills";
    public string NoScpItems { get; set; } = "No se recogieron objetos <color=red>SCP</color>";
    public string NoEscape { get; set; } = "Nadie escapó de la Instalacion";
}
