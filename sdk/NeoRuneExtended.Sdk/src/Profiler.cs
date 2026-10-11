#if NEORUNE_PROFILE
using System.Collections.Generic;
using UE.CoreUObject;
using UE.Engine;

namespace NeoRune
{
    /// <summary>Storage for <see cref="Profiler"/>: SaveGames/NeoRune_&lt;Mod&gt;_Perf.sav.</summary>
    public class NeoRunePerfData : USaveGame
    {
        public List<string> Lines = new();
    }

    /// <summary>
    /// Profiling builds only (<c>dotnet build -p:NeoRuneProfile=true</c>): every function of the mod counts its calls and
    /// time, and this saves the counters every 5 seconds for "neorunex perf". The compiler starts it with the mod; there is
    /// nothing to call. Normal builds don't contain it, so they don't pay for it.
    /// </summary>
    public class Profiler : UObject
    {
        public static void Start(UObject context)
        {
            // The counters last the whole session (they are static fields): the session starts with the first level.
            if (NeoRuneProfile.Counters.StartFrame == 0)
            {
                NeoRuneProfile.Counters.StartFrame = UKismetSystemLibrary.GetFrameCount();
                NeoRuneProfile.Counters.StartTime = Now();
            }
            // One profiler for the session, held by a static field (else garbage collection removes it, found in game)
            // and owned by the game instance; its timer belongs to the level, so each level sets it again.
            if (NeoRuneProfile.Counters.Instance == null)
            {
                UObject owner = UGameplayStatics.GetGameInstance(context) ?? context;
                NeoRuneProfile.Counters.Instance = UGameplayStatics.SpawnObject(Unreal.ClassOf<Profiler>(), owner) as Profiler;
            }
            if (NeoRuneProfile.Counters.Instance != null)
                UKismetSystemLibrary.K2_SetTimer(NeoRuneProfile.Counters.Instance, "Flush", 5f, true, false, 0f, 0f);
        }

        void Flush()
        {
            var data = UGameplayStatics.CreateSaveGameObject(Unreal.ClassOf<NeoRunePerfData>()) as NeoRunePerfData;
            if (data == null) return;
            var lines = new List<string>();
            lines.Add($"session {Now() - NeoRuneProfile.Counters.StartTime} {UKismetSystemLibrary.GetFrameCount() - NeoRuneProfile.Counters.StartFrame}");
            lines.AddRange(NeoRuneProfile.Counters.Snapshot());
            data.Lines = lines;
            UGameplayStatics.SaveGameToSlot(data, "NeoRune_" + Unreal.ModName + "_Perf", 0);
        }

        static double Now()
        {
            UGameplayStatics.GetAccurateRealTime(out var seconds, out var part);
            return seconds + part;
        }
    }
}
#endif
