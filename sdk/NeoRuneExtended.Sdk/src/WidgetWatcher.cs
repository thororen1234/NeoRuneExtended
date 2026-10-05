using System.Collections.Generic;
using UE.CoreUObject;
using UE.Engine;
using UE.UMG;

namespace NeoRune
{
    /// <summary>The handler of <see cref="WidgetWatcher.Found"/>: a method taking the widget.</summary>
    [UDelegate("/Script/UMG.OnListEntryGeneratedDynamic__DelegateSignature")]
    public delegate void WidgetEvent(UUserWidget widget);

    /// <summary>
    /// Reports each widget of a class once, as the game creates it: hook its buttons or add your own children in the
    /// handler, and it won't run again for the same widget. Unreal has no "widget created" event, so it checks a few times
    /// a second (also while the game is paused).
    /// <code>
    /// menus = WidgetWatcher.Start(this, Unreal.ClassOf&lt;UAS_ContextMenu_Activatable&gt;(), 0.25f);
    /// menus.Found += OnMenu;   // void OnMenu(UUserWidget widget)
    /// </code>
    /// It's an actor in the level: it ends with the level, or call <see cref="Stop"/>.
    /// </summary>
    public class WidgetWatcher : AActor
    {
        public event WidgetEvent Found;
        TSubclassOf<UUserWidget> widgetClass;
        List<UUserWidget> seen = new();
        PausableTimer? timer;

        public static WidgetWatcher? Start(UObject context, TSubclassOf<UUserWidget> widgetClass, float seconds)
        {
            var watcher = World.Spawn(context, Unreal.ClassOf<WidgetWatcher>(), new FVector()) as WidgetWatcher;
            watcher?.Watch(widgetClass, seconds);
            return watcher;
        }

        /// <summary>Stops watching and removes the watcher.</summary>
        public void Stop()
        {
            timer?.Stop();
            K2_DestroyActor();
        }

        void Watch(TSubclassOf<UUserWidget> cls, float seconds)
        {
            widgetClass = cls;
            timer = PausableTimer.Start(this, seconds, loop: true);
            if (timer != null) timer.Fired += Scan;
        }

        void Scan()
        {
            // Forget widgets the game has destroyed, so the list doesn't grow.
            for (int i = seen.Count - 1; i >= 0; i--)
                if (!UKismetSystemLibrary.IsValid(seen[i])) seen.RemoveAt(i);
            UWidgetBlueprintLibrary.GetAllWidgetsOfClass(this, out var widgets, widgetClass, false);
            foreach (var widget in widgets)
            {
                if (seen.Contains(widget)) continue;
                seen.Add(widget);
                Found(widget);
            }
        }
    }
}
