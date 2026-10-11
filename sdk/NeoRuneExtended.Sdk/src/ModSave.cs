using UE.Engine;

namespace NeoRune
{
    /// <summary>
    /// Saved data of your own, kept across sessions and restarts: any struct (yours, with lists and dictionaries in it,
    /// or a game struct), saved under a name.
    /// <code>
    /// public struct Progress { public int Kills; public List&lt;string&gt; Found; }
    ///
    /// if (!ModSave.Load("progress", out Progress progress)) progress = new Progress { Found = new() };
    /// progress.Kills++;
    /// ModSave.Save("progress", progress);
    /// </code>
    /// Each save is a file (SaveGames/NeoRune_&lt;Mod&gt;_&lt;name&gt;.sav, or the Xbox app's save storage). Fields are saved
    /// by name, so a later version of the mod can add or remove fields: the others still load. Saving writes the file,
    /// so save when something changed, not every frame. For settings the player picks, use <see cref="ModSettings"/>.
    /// </summary>
    public static class ModSave
    {
        /// <summary>Reads the data saved under <paramref name="name"/>; false (and the default value) when there is none.</summary>
        [Intrinsic("save-load")]
        public static bool Load<T>(string name, out T data) where T : struct => throw null!;

        /// <summary>Saves the data under <paramref name="name"/>; false if the game couldn't write it.</summary>
        [Intrinsic("save-write")]
        public static bool Save<T>(string name, T data) where T : struct => throw null!;

        public static bool Exists(string name) => UGameplayStatics.DoesSaveGameExist(Slot(name), 0);

        public static void Delete(string name) => UGameplayStatics.DeleteGameInSlot(Slot(name), 0);

        /// <summary>The save slot of a name: NeoRune_&lt;Mod&gt;_&lt;name&gt;.</summary>
        public static string Slot(string name) => "NeoRune_" + Unreal.ModName + "_" + name;
    }
}
