namespace CandyUtilities;

using InventorySystem.Items.Usables.Scp330;
using System.ComponentModel;

public sealed class Translation 
{
    [Description("The text shown when picking up a candy, note %type% gets replaced with the type of candy")]
    public string PickupText { get; set; } = "Tomaste un caramelo %type%.";

    [Description("The text shown below pickup text to show the maximum candies, use {amount} as placeholder")]
    public string MaxCandiesText { get; set; } = "<color=red>Máximo {amount}</color>";

    [Description("Dictionary of candies and the respective text to show based on them")]
    public Dictionary<CandyKindID, string> CandyText { get; set; } = new()
    {
        { CandyKindID.Rainbow, "<color=#FF0000>A</color><color=#FF7F00>R</color><color=#FFFF00>C</color><color=#00FF00>O</color><color=#0000FF>I</color><color=#4B0082>R</color><color=#8A2BE2>I</color><color=#FF0000>S</color>" },
        { CandyKindID.Yellow, "<color=#FFFF00>Amarillo</color>" },
        { CandyKindID.Purple, "<color=#800080>Morado</color>" },
        { CandyKindID.Red, "<color=#FF0000>Rojo</color>" },
        { CandyKindID.Green, "<color=#008000>Verde</color>" },
        { CandyKindID.Blue, "<color=#0000FF>Azul</color>" },
        { CandyKindID.Pink, "<color=#FFC0CB>Rosa</color>" },
        { CandyKindID.Orange, "<color=#FFA500>Naranja</color>" },
        { CandyKindID.White, "<color=#FFFFFF>Blanco</color>" },
        { CandyKindID.Gray, "<color=#808080>Gris</color>" },
        { CandyKindID.Black, "<color=#000000>Negro</color>" },
        { CandyKindID.Brown, "<color=#FFA500>Marrón</color>" },
        { CandyKindID.Evil, "<color=#FF0000>Mal</color><color=000000>vado</color>" },
    };
}