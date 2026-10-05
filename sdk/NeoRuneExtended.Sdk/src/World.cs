using System.Collections.Generic;
using UE.CoreUObject;
using UE.Engine;

namespace NeoRune
{
    /// <summary>Common lookups in the current level.</summary>
    public static class World
    {
        /// <summary>The local player's pawn (null in menus without a character).</summary>
        public static APawn? Player(UObject context) => UGameplayStatics.GetPlayerPawn(context, 0);

        public static APlayerController? PlayerController(UObject context) => UGameplayStatics.GetPlayerController(context, 0);

        /// <summary>Name of the current level, e.g. "Menu_Spicewood".</summary>
        public static string LevelName(UObject context) => UGameplayStatics.GetCurrentLevelName(context, true);

        /// <summary>Seconds since the level started, unaffected by pause.</summary>
        public static double RealTime(UObject context) => UGameplayStatics.GetRealTimeSeconds(context);

        /// <summary>All actors of a class in the level.</summary>
        public static List<AActor> FindAll(UObject context, TSubclassOf<AActor> actorClass)
        {
            UGameplayStatics.GetAllActorsOfClass(context, actorClass, out var actors);
            return actors;
        }

        /// <summary>Spawns an actor of the given class at a location.</summary>
        public static AActor? Spawn(UObject context, TSubclassOf<AActor> actorClass, FVector location)
        {
            var transform = UKismetMathLibrary.MakeTransform(location, new FRotator(), new FVector { X = 1, Y = 1, Z = 1 });
            var actor = UGameplayStatics.BeginDeferredActorSpawnFromClass(context, actorClass, transform, ESpawnActorCollisionHandlingMethod.AlwaysSpawn, null, ESpawnActorScaleMethod.MultiplyWithRoot);
            if (actor == null) return null;
            return UGameplayStatics.FinishSpawningActor(actor, transform, ESpawnActorScaleMethod.MultiplyWithRoot);
        }
    }
}
