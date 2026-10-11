using System.Collections.Generic;
using UE.CoreUObject;
using UE.Engine;

namespace NeoRune
{
    /// <summary>Storage for <see cref="Trail"/>: SaveGames/NeoRune_&lt;Mod&gt;_Trail.sav.</summary>
    public class NeoRuneTrailData : USaveGame
    {
        public List<string> Steps = new();
    }

    /// <summary>
    /// Breadcrumbs for crash reports: the last few things a mod did, kept on disk so they survive a crash.
    /// "neorunex crash" lines them up with the game's newest crash report and names the mod (and the step) that was running.
    /// <para>
    /// Every NeoRune mod already leaves one when its ModActor starts in a level, and the SDK marks its risky steps (adding
    /// widgets to the game's UI, opening menus). Mark your own risky code the same way:
    /// <code>
    /// Trail.Begin("spawn pets");
    /// SpawnPets();
    /// Trail.End("spawn pets");
    /// </code>
    /// A step that began but never ended is what the crash interrupted. Each call saves a small file, so use it for things
    /// that happen now and then (setup, level changes, menus), never every frame.
    /// </para>
    /// </summary>
    public static class Trail
    {
        /// <summary>How many steps the file keeps (the newest ones).</summary>
        const int Keep = 24;

        public static void Begin(string step) => Write("begin " + step);

        public static void End(string step) => Write("end " + step);

        /// <summary>Records that something happened (no matching End needed).</summary>
        public static void Mark(string what) => Write(what);

        static void Write(string text)
        {
            var slot = "NeoRune_" + Unreal.ModName + "_Trail";
            var data = UGameplayStatics.LoadGameFromSlot(slot, 0) as NeoRuneTrailData;
            if (data == null) data = UGameplayStatics.CreateSaveGameObject(Unreal.ClassOf<NeoRuneTrailData>()) as NeoRuneTrailData;
            if (data == null) return;
            var steps = data.Steps;
            while (steps.Count >= Keep) steps.RemoveAt(0);
            // Unix seconds (UTC), so the command line can line the steps up with the crash time.
            steps.Add($"{UKismetMathLibrary.ToUnixTimestamp(UKismetMathLibrary.UtcNow())} {text}");
            data.Steps = steps;
            UGameplayStatics.SaveGameToSlot(data, slot, 0);
        }
    }
}
