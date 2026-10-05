using UE.CoreUObject;
using UE.Engine;
using UE.Minimap;
using UE.SlateCore;
using UE.UMG;

namespace NeoRune
{
    /// <summary>
    /// Building widgets in code. Each widget needs an outer: the widget tree it goes into (<see cref="ModMenu"/> and
    /// <see cref="ModWidget"/> pass theirs to Build). Text uses the game's font, since the engine's default font isn't always
    /// in the game.
    /// </summary>
    public static class Ui
    {
        /// <summary>A widget's tree, made if it has none yet (widgets made in code start without one).</summary>
        public static UWidgetTree? Tree(UUserWidget widget)
        {
            if (widget.WidgetTree == null) widget.WidgetTree = UGameplayStatics.SpawnObject(Unreal.ClassOf<UWidgetTree>(), widget) as UWidgetTree;
            return widget.WidgetTree;
        }

        /// <summary>
        /// Creates a widget of a class for the local player: one of yours (a <see cref="ModWidget"/>), or one of the game's
        /// by its class (see <see cref="GameWidget"/>).
        /// </summary>
        public static UUserWidget? Create(UObject context, TSubclassOf<UUserWidget> widgetClass) =>
            UWidgetBlueprintLibrary.Create(context, widgetClass, World.PlayerController(context));

        /// <summary>
        /// Creates one of the game's widgets by its class path, e.g. a button, so yours look like the game's. Null when the
        /// class doesn't exist (game updates can move them). <see cref="GameUI.Dump"/> shows the class of each widget on screen.
        /// </summary>
        public static UUserWidget? GameWidget(UObject context, string classPath)
        {
            var widgetClass = Unreal.LoadClass<UUserWidget>(classPath);
            if (widgetClass == null) return null;
            return Create(context, widgetClass);
        }

        /// <summary>Text in the game's font.</summary>
        public static UTextBlock? Text(UObject? outer, string text, int size) => Text(outer, text, size, Color(1, 1, 1, 1));

        /// <summary>Text in the game's font, in a colour.</summary>
        public static UTextBlock? Text(UObject? outer, string text, int size, FLinearColor color)
        {
            var label = UGameplayStatics.SpawnObject(Unreal.ClassOf<UTextBlock>(), outer) as UTextBlock;
            if (label == null) return null;
            label.SetText(text);
            var font = UMinimapHelpersLibrary.GetDefaultFont();
            if (font.FontObject != null)
            {
                font.Size = size;
                label.SetFont(font);
            }
            label.SetColorAndOpacity(SlateColor(color));
            return label;
        }

        /// <summary>Makes text look like one of the game's text blocks: its font, size and colour.</summary>
        public static void StyleLike(UTextBlock? text, UTextBlock? gameText)
        {
            if (text == null || gameText == null) return;
            text.SetFont(gameText.Font);
            text.SetColorAndOpacity(gameText.ColorAndOpacity);
        }

        /// <summary>Children one under the other.</summary>
        public static UVerticalBox? Column(UObject? outer) => UGameplayStatics.SpawnObject(Unreal.ClassOf<UVerticalBox>(), outer) as UVerticalBox;

        /// <summary>Children side by side.</summary>
        public static UHorizontalBox? Row(UObject? outer) => UGameplayStatics.SpawnObject(Unreal.ClassOf<UHorizontalBox>(), outer) as UHorizontalBox;

        /// <summary>Content on a coloured background, with padding around it.</summary>
        public static UBorder? Panel(UObject? outer, UWidget? content, FLinearColor background, float padding)
        {
            var panel = UGameplayStatics.SpawnObject(Unreal.ClassOf<UBorder>(), outer) as UBorder;
            if (panel == null) return null;
            panel.SetBrushColor(background);
            panel.SetPadding(new FMargin { Left = padding, Top = padding, Right = padding, Bottom = padding });
            if (content != null) panel.AddChild(content);
            return panel;
        }

        /// <summary>A plain button with a label. Subscribe to its OnClicked.</summary>
        public static UButton? Button(UObject? outer, string label, int size)
        {
            var button = UGameplayStatics.SpawnObject(Unreal.ClassOf<UButton>(), outer) as UButton;
            var text = Text(outer, label, size, Color(0.1f, 0.1f, 0.1f, 1));
            if (button == null) return null;
            if (text != null) button.AddChild(text);
            return button;
        }

        /// <summary>A colour from red, green, blue and alpha (0 to 1).</summary>
        public static FLinearColor Color(float r, float g, float b, float a) => new FLinearColor { R = r, G = g, B = b, A = a };

        /// <summary>A colour as the widgets' colour type.</summary>
        public static FSlateColor SlateColor(FLinearColor color) =>
            new FSlateColor { SpecifiedColor = color, ColorUseRule = ESlateColorStylingMode.UseColor_Specified };

        /// <summary>The same padding on every side.</summary>
        public static FMargin Margin(float all) => new FMargin { Left = all, Top = all, Right = all, Bottom = all };
    }
}
