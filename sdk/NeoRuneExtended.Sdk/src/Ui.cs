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

        /// <summary>Creates a widget of a class for the local player: one of yours (a <see cref="ModWidget"/>) or one of the game's.</summary>
        public static UUserWidget? Create(UObject context, TSubclassOf<UUserWidget> widgetClass) =>
            UWidgetBlueprintLibrary.Create(context, widgetClass, World.PlayerController(context));

        /// <summary>
        /// Creates one of the game's widgets by its class path, e.g. so yours look like the game's. Null when the class doesn't
        /// exist (game updates can move them). <see cref="GameUI.Dump"/> logs the class of each widget on screen.
        /// </summary>
        public static UUserWidget? GameWidget(UObject context, string classPath)
        {
            var widgetClass = Unreal.LoadClass<UUserWidget>(classPath);
            return widgetClass != null ? Create(context, widgetClass) : null;
        }

        /// <summary>White text in the game's font.</summary>
        public static UTextBlock? Text(UObject? outer, string text, int size) => Text(outer, text, size, Color(1, 1, 1, 1));

        /// <summary>Text in the game's font, in a colour.</summary>
        public static UTextBlock? Text(UObject? outer, string text, int size, FLinearColor color)
        {
            var label = UGameplayStatics.SpawnObject(Unreal.ClassOf<UTextBlock>(), outer) as UTextBlock;
            if (label == null) return null;
            label.SetText(text);
            var font = UMinimapHelpersLibrary.GetDefaultFont();
            if (font.FontObject == null) font = label.Font;   // the game's font isn't loaded yet on the main menu
            font.Size = size;
            label.SetFont(font);
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

        /// <summary>
        /// A picture (e.g. from <see cref="Images.Load"/>) at a size in pixels; 0, 0 is the texture's own size. It's a box with
        /// the image centred inside, so it keeps its size in a column or row instead of stretching to fill it.
        /// </summary>
        public static USizeBox? Image(UObject? outer, UTexture2D? texture, float width, float height)
        {
            var box = UGameplayStatics.SpawnObject(Unreal.ClassOf<USizeBox>(), outer) as USizeBox;
            var image = UGameplayStatics.SpawnObject(Unreal.ClassOf<UImage>(), outer) as UImage;
            if (box == null || image == null) return null;
            if (width <= 0 || height <= 0)
            {
                width = texture != null ? texture.Blueprint_GetSizeX() : 0;
                height = texture != null ? texture.Blueprint_GetSizeY() : 0;
            }
            // The brush's size is the image's size (a size override on the image itself isn't used), and the image is
            // centred in the box: a column or row stretches the box, never the picture.
            image.SetBrushFromTexture(texture, false);
            var brush = image.Brush;
            brush.ImageSize = new UE.SlateCore.FDeprecateSlateVector2D { X = width, Y = height };
            image.SetBrush(brush);
            box.SetWidthOverride(width);
            box.SetHeightOverride(height);
            var slot = box.AddChild(image) as USizeBoxSlot;
            slot?.SetHorizontalAlignment(EHorizontalAlignment.HAlign_Center);
            slot?.SetVerticalAlignment(EVerticalAlignment.VAlign_Center);
            return box;
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
            panel.SetPadding(Margin(padding));
            if (content != null) panel.AddChild(content);
            return panel;
        }

        /// <summary>A plain button with a label. Subscribe to its OnClicked.</summary>
        public static UButton? Button(UObject? outer, string label, int size)
        {
            var button = UGameplayStatics.SpawnObject(Unreal.ClassOf<UButton>(), outer) as UButton;
            if (button == null) return null;
            button.SetBackgroundColor(Color(0.15f, 0.15f, 0.15f, 1));
            var text = Text(outer, label, size);
            if (text != null) button.AddChild(text);
            return button;
        }

        /// <summary>
        /// Puts content in the middle of the space it gets (a menu's whole screen), <paramref name="width"/> wide (0: as wide as
        /// the content wants).
        /// </summary>
        public static UOverlay? Center(UObject? outer, UWidget? content, float width)
        {
            var overlay = UGameplayStatics.SpawnObject(Unreal.ClassOf<UOverlay>(), outer) as UOverlay;
            if (overlay == null || content == null) return overlay;
            UWidget child = content;
            if (width > 0 && UGameplayStatics.SpawnObject(Unreal.ClassOf<USizeBox>(), outer) is USizeBox box)
            {
                box.SetWidthOverride(width);
                box.AddChild(content);
                child = box;
            }
            var slot = overlay.AddChildToOverlay(child);
            slot?.SetHorizontalAlignment(EHorizontalAlignment.HAlign_Center);
            slot?.SetVerticalAlignment(EVerticalAlignment.VAlign_Center);
            return overlay;
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
