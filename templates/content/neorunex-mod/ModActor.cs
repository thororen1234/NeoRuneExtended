using NeoRune;
using UE.Engine;

namespace MyMod;

/// <summary>
/// The mod loader (BetterBlueprintLoader or Blueprint Loader) spawns ModActor every time a level loads, including the
/// main menu. Override game events like ReceiveBeginPlay / ReceiveTick and call game functions through the UE.*
/// namespaces. See the NeoRuneExtended docs for what C# is supported.
/// </summary>
public class ModActor : AActor
{
    int loads;

    protected override void ReceiveBeginPlay()
    {
        loads++;
        Log.Write($"MyMod loaded in {World.LevelName(this)}");
        // Read the log with:  neorunex log MyMod
    }
}
