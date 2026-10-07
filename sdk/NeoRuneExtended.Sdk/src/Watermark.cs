using UE.CoreUObject;
using UE.Engine;
using UE.Minimap;
using UE.SlateCore;
using UE.UMG;

namespace NeoRune
{
    /// <summary>
    /// The faint "NeoRuneExtended mods loaded" mark at the top left of the main menu. Every NeoRune mod shows it (the compiler calls
    /// <see cref="Show"/> at the start of ModActor.ReceiveBeginPlay), but only once on screen however many mods are installed.
    /// </summary>
    public class NeoRuneWatermark : ScreenWidget
    {
        /// <summary>Actor tag of every NeoRune mod's ModActor, so the mark can count them.</summary>
        const string ModTag = "NeoRuneMod";

        UTextBlock? text;

        public static void Show(UObject context)
        {
            if (context is AActor modActor)
            {
                var tags = modActor.Tags;
                tags.Add(ModTag);
                modActor.Tags = tags;
            }
            if (!World.LevelName(context).StartsWith("Menu")) return;
            // Take over from a mark already shown: it may be from a mod built before the count (those never replace a
            // shown mark). Collapsed rather than removed, since a shown ScreenWidget puts itself back.
            HideOthers(context);
            var mark = UWidgetBlueprintLibrary.Create(context, Unreal.ClassOf<NeoRuneWatermark>(), World.PlayerController(context)) as NeoRuneWatermark;
            if (mark == null || !mark.Build()) return;
            mark.ShowAt(new FVector2D { X = 16, Y = 16 });
            // Mods are spawned one after another: recount until they've all started.
            mark.Count();
            Timer.Start(mark, nameof(Count), 1f, loop: true);
        }

        /// <summary>Hides the marks other mods showed (each mod has its own copy of this class, so compare by name).</summary>
        static void HideOthers(UObject context)
        {
            UWidgetBlueprintLibrary.GetAllWidgetsOfClass(context, out var widgets, Unreal.ClassOf<UUserWidget>(), true);
            foreach (var widget in widgets)
                if (UKismetSystemLibrary.GetClassDisplayName(UGameplayStatics.GetObjectClass(widget)).StartsWith("NeoRuneWatermark"))
                    widget.SetVisibility(ESlateVisibility.Collapsed);
        }

        void Count()
        {
            UGameplayStatics.GetAllActorsWithTag(this, ModTag, out var mods);
            text?.SetText(mods.Count == 1 ? "1 NeoRuneExtended mod loaded" : $"{mods.Count} NeoRuneExtended mods loaded");
        }

        bool Build()
        {
            var tree = WidgetTree;
            if (tree == null)
            {
                tree = UGameplayStatics.SpawnObject(Unreal.ClassOf<UWidgetTree>(), this) as UWidgetTree;
                WidgetTree = tree;
            }
            text = UGameplayStatics.SpawnObject(Unreal.ClassOf<UTextBlock>(), tree) as UTextBlock;
            if (tree == null || text == null) return false;
            var font = UMinimapHelpersLibrary.GetDefaultFont();
            if (font.FontObject == null) font = text.Font;   // the game's font isn't loaded yet on the main menu
            font.Size = 24;
            text.SetFont(font);
            text.SetColorAndOpacity(new FSlateColor
            {
                SpecifiedColor = new FLinearColor { R = 1, G = 1, B = 1, A = 0.45f },
                ColorUseRule = ESlateColorStylingMode.UseColor_Specified,
            });
            tree.RootWidget = text;
            return true;
        }
    }
}
