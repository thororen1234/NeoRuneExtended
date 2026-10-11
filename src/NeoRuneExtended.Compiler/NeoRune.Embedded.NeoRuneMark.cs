// Compiled into every NeoRune mod by the compiler (it is not part of the SDK sources, and mods can't refer to it).
using UE.CoreUObject;
using UE.Engine;
using UE.Minimap;
using UE.SlateCore;
using UE.UMG;

namespace NeoRune
{
    /// <summary>
    /// The faint "N NeoRuneExtended mods loaded" mark at the top left of the main menu. Every NeoRune mod's ModActor calls
    /// <see cref="Show"/> first thing in ReceiveBeginPlay, and every mod brings its own copy of this class, but only one
    /// mark is on screen: each second the marks pick the same one (the first in the engine's object list), which shows
    /// itself and the count, and hides the others, including the marks of mods built before 1.0.
    /// Only the game's own functions are used, so it doesn't depend on SDK helpers.
    /// </summary>
    public class NeoRuneMark : UUserWidget
    {
        /// <summary>Actor tag of every NeoRune mod's ModActor, so the mark can count them.</summary>
        const string ModTag = "NeoRuneMod";

        UTextBlock? label;

        public static void Show(UObject context)
        {
            if (context is AActor modActor)
            {
                var tags = modActor.Tags;
                tags.Add(ModTag);
                modActor.Tags = tags;
            }
            // Keeps the mod's ModActor class loaded for the whole session: its class default object holds the mod's static
            // fields, and without a reference the class is unloaded with the level and comes back fresh (checked in game).
            var instance = UGameplayStatics.GetGameInstance(context);
            if (instance != null)
            {
                var kept = instance.ReferencedObjects;
                var modClass = AsObject(UGameplayStatics.GetObjectClass(context));
                if (modClass != null && !kept.Contains(modClass))
                {
                    kept.Add(modClass);
                    instance.ReferencedObjects = kept;
                }
            }
            // The mod's breadcrumb for crash reports ("neorune crash"): which level it started in.
            Trail.Mark("start " + UGameplayStatics.GetCurrentLevelName(context, true));
            if (!UGameplayStatics.GetCurrentLevelName(context, true).StartsWith("Menu")) return;
            var player = UGameplayStatics.GetPlayerController(context, 0);
            var mark = UWidgetBlueprintLibrary.Create(context, Unreal.ClassOf<NeoRuneMark>(), player) as NeoRuneMark;
            if (mark == null || !mark.Build()) return;
            mark.Guard();
            UKismetSystemLibrary.K2_SetTimer(mark, "Guard", 1f, true, false, 0f, 0f);
        }

        /// <summary>Once a second on the main menu: the chosen mark shows itself with the count, the others hide.</summary>
        void Guard()
        {
            UWidgetBlueprintLibrary.GetAllWidgetsOfClass(this, out var widgets, Unreal.ClassOf<UUserWidget>(), false);
            UUserWidget? chosen = null;
            foreach (var widget in widgets)
                if (UKismetSystemLibrary.GetClassDisplayName(UGameplayStatics.GetObjectClass(widget)).StartsWith("NeoRuneMark"))
                {
                    chosen = widget;
                    break;
                }
            foreach (var widget in widgets)
            {
                var name = UKismetSystemLibrary.GetClassDisplayName(UGameplayStatics.GetObjectClass(widget));
                // Marks of mods built before 1.0 are hidden for good (they don't pick one themselves).
                if (widget != chosen && (name.StartsWith("NeoRuneMark") || name.StartsWith("NeoRuneWatermark")))
                    widget.SetVisibility(ESlateVisibility.Collapsed);
            }
            if (chosen != this) return;

            UGameplayStatics.GetAllActorsWithTag(this, ModTag, out var mods);
            label?.SetText(mods.Count == 1 ? "1 NeoRuneExtended mod loaded" : $"{mods.Count} NeoRuneExtended mods loaded");
            label?.SetVisibility(ESlateVisibility.HitTestInvisible);
            label?.SetRenderOpacity(1f);
            SetVisibility(ESlateVisibility.HitTestInvisible);
            SetRenderOpacity(1f);
            // Under the game's UI, like the game's own widgets; put back if something took it off the screen.
            if (!IsInViewport())
            {
                var player = UGameplayStatics.GetPlayerController(this, 0);
                if (player == null) return;
                SetOwningPlayer(player);
                AddToPlayerScreen(-10);
            }
            SetPositionInViewport(new FVector2D { X = 16, Y = 16 }, false);
        }

        /// <summary>A class as the object it is (compiler built-in: the same pointer).</summary>
        [Intrinsic("class-object")]
        static UObject? AsObject(TSubclassOf<UObject> cls) => throw null!;

        bool Build()
        {
            var tree = WidgetTree;
            if (tree == null)
            {
                tree = UGameplayStatics.SpawnObject(Unreal.ClassOf<UWidgetTree>(), this) as UWidgetTree;
                WidgetTree = tree;
            }
            label = UGameplayStatics.SpawnObject(Unreal.ClassOf<UTextBlock>(), tree) as UTextBlock;
            if (tree == null || label == null) return false;
            var font = UMinimapHelpersLibrary.GetDefaultFont();
            if (font.FontObject == null) font = label.Font;   // the game's font isn't loaded yet on the main menu
            font.Size = 24;
            label.SetFont(font);
            label.SetColorAndOpacity(new FSlateColor
            {
                SpecifiedColor = new FLinearColor { R = 1, G = 1, B = 1, A = 0.45f },
                ColorUseRule = ESlateColorStylingMode.UseColor_Specified,
            });
            tree.RootWidget = label;
            return true;
        }
    }
}
