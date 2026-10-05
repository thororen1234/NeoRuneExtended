using UE.CoreUObject;
using UE.Engine;

namespace NeoRune
{
    /// <summary>Debugging helpers that write to the mod's log (see <see cref="Log"/>).</summary>
    public static class Debug
    {
        /// <summary>
        /// Logs an actor and each of its components: class, mesh, attach parent and socket, relative transform and visibility.
        /// The quickest way to see how the game builds something, e.g. which socket a piece of armor is attached to.
        /// </summary>
        public static void DumpActor(AActor? actor)
        {
            if (actor == null) { Log.Write("DumpActor: null actor"); return; }
            var line = $"Actor {UKismetSystemLibrary.GetObjectName(actor)} ({ClassName(actor)}) at {UKismetStringLibrary.Conv_VectorToString(actor.K2_GetActorLocation())}";
            var parent = actor.GetAttachParentActor();
            if (parent != null) line += $", attached to {UKismetSystemLibrary.GetObjectName(parent)} socket {actor.GetAttachParentSocketName()}";
            Log.Write(line);
            foreach (var component in actor.K2_GetComponentsByClass(Unreal.ClassOf<UActorComponent>()))
                Log.Write("  " + Describe(component));
        }

        static string ClassName(UObject o) => UKismetSystemLibrary.GetClassDisplayName(UGameplayStatics.GetObjectClass(o));

        static string Describe(UActorComponent component)
        {
            var text = $"{UKismetSystemLibrary.GetObjectName(component)} ({ClassName(component)})";
            if (component is UStaticMeshComponent staticMesh && staticMesh.StaticMesh != null)
                text += $" mesh={UKismetSystemLibrary.GetObjectName(staticMesh.StaticMesh)}";
            if (component is USkinnedMeshComponent skinned && skinned.GetSkinnedAsset() != null)
                text += $" mesh={UKismetSystemLibrary.GetObjectName(skinned.GetSkinnedAsset())}";
            if (component is USceneComponent scene)
            {
                var parent = scene.GetAttachParent();
                if (parent != null) text += $" parent={UKismetSystemLibrary.GetObjectName(parent)} socket={scene.GetAttachSocketName()}";
                text += $" relative=[{UKismetStringLibrary.Conv_TransformToString(scene.GetRelativeTransform())}] visible={(scene.IsVisible() ? "yes" : "no")}";
            }
            return text;
        }
    }
}
