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
        public static void Show(UObject context)
        {
            if (!World.LevelName(context).StartsWith("Menu") || IsShown(context)) return;
            var mark = UWidgetBlueprintLibrary.Create(context, Unreal.ClassOf<NeoRuneWatermark>(), World.PlayerController(context)) as NeoRuneWatermark;
            if (mark != null && mark.Build()) mark.ShowAt(new FVector2D { X = 16, Y = 16 });
        }

        /// <summary>Another mod's copy of this class is already on screen (each mod has its own, so compare by name).</summary>
        static bool IsShown(UObject context)
        {
            UWidgetBlueprintLibrary.GetAllWidgetsOfClass(context, out var widgets, Unreal.ClassOf<UUserWidget>(), true);
            foreach (var widget in widgets)
                if (UKismetSystemLibrary.GetClassDisplayName(UGameplayStatics.GetObjectClass(widget)).StartsWith("NeoRuneWatermark")) return true;
            return false;
        }

        bool Build()
        {
            var tree = WidgetTree;
            if (tree == null)
            {
                tree = UGameplayStatics.SpawnObject(Unreal.ClassOf<UWidgetTree>(), this) as UWidgetTree;
                WidgetTree = tree;
            }
            var text = UGameplayStatics.SpawnObject(Unreal.ClassOf<UTextBlock>(), tree) as UTextBlock;
            if (tree == null || text == null) return false;
            var font = UMinimapHelpersLibrary.GetDefaultFont();
            if (font.FontObject != null)
            {
                font.Size = 14;
                text.SetFont(font);
            }
            text.SetText("NeoRuneExtended mods loaded");
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
