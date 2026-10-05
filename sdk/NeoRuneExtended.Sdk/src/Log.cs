using System.Collections.Generic;
using UE.CoreUObject;
using UE.Engine;
using UE.MainMenu;
using UE.Minimap;
using UE.SlateCore;
using UE.UMG;

namespace NeoRune
{
    /// <summary>Storage for <see cref="Log"/>: SaveGames/NeoRune_&lt;Mod&gt;.sav.</summary>
    public class NeoRuneLogData : USaveGame
    {
        public List<string> Lines = new();
    }

    /// <summary>
    /// Debug log for mods. Shipping builds ignore Print String, so lines are appended to
    /// %LOCALAPPDATA%\Dungeons2\Saved\SaveGames\NeoRune_&lt;Mod&gt;.sav (Steam) or the Xbox app version's save storage;
    /// read them with "neorune log &lt;Mod&gt;", which looks in both, or on screen with <see cref="Show"/>.
    /// Each Write saves the file: to log every frame, use a <see cref="LogBuffer"/>.
    /// </summary>
    public static class Log
    {
        public static void Write(string line) => WriteAll(new List<string> { line });

        /// <summary>Appends several lines with one save.</summary>
        public static void WriteAll(List<string> lines)
        {
            var slot = Slot();
            var data = UGameplayStatics.LoadGameFromSlot(slot, 0) as NeoRuneLogData;
            if (data == null) data = UGameplayStatics.CreateSaveGameObject(Unreal.ClassOf<NeoRuneLogData>()) as NeoRuneLogData;
            if (data == null) return;
            foreach (var line in lines) data.Lines.Add(line);
            UGameplayStatics.SaveGameToSlot(data, slot, 0);
        }

        /// <summary>The lines logged so far.</summary>
        public static List<string> ReadLines()
        {
            var data = UGameplayStatics.LoadGameFromSlot(Slot(), 0) as NeoRuneLogData;
            return data != null ? data.Lines : new List<string>();
        }

        /// <summary>Deletes the log file (e.g. at the start of a session).</summary>
        public static void Clear() => UGameplayStatics.DeleteGameInSlot(Slot(), 0);

        /// <summary>
        /// Shows the log on screen (last lines, refreshed every second) with Copy and Close buttons. Copy puts the whole
        /// log on the clipboard, so players can paste it into a bug report, also on the Xbox app version.
        /// </summary>
        public static LogViewer? Show(UObject context) => LogViewer.Open(context);

        static string Slot() => "NeoRune_" + Unreal.ModName;
    }

    /// <summary>
    /// Collects log lines in memory and saves them at most every 2 seconds, so a mod can log from code that runs every
    /// frame. Keep it in a field: <c>log = LogBuffer.Create(this);</c>, then <c>log.Write(...)</c>. Lines not saved yet are
    /// lost when the level unloads: call <see cref="Flush"/> in ReceiveEndPlay.
    /// </summary>
    public class LogBuffer : UObject
    {
        public List<string> Pending = new();
        UObject? owner;
        double lastSave;

        public static LogBuffer? Create(UObject owner)
        {
            var buffer = UGameplayStatics.SpawnObject(Unreal.ClassOf<LogBuffer>(), owner) as LogBuffer;
            if (buffer != null) buffer.owner = owner;
            return buffer;
        }

        public void Write(string line)
        {
            Pending.Add(line);
            if (owner != null && World.RealTime(owner) - lastSave >= 2) Flush();
        }

        /// <summary>Saves the pending lines now.</summary>
        public void Flush()
        {
            if (Pending.Count == 0) return;
            Log.WriteAll(Pending);
            Pending.Clear();
            if (owner != null) lastSave = World.RealTime(owner);
        }
    }

    /// <summary>The on-screen log opened by <see cref="Log.Show"/>.</summary>
    public class LogViewer : ScreenWidget
    {
        const int VisibleLines = 30;
        UTextBlock? body;

        public static LogViewer? Open(UObject context)
        {
            var viewer = UWidgetBlueprintLibrary.Create(context, Unreal.ClassOf<LogViewer>(), World.PlayerController(context)) as LogViewer;
            if (viewer == null || !viewer.Build()) return null;
            // Above the game's UI, at the bottom right: after logging in, the main menu takes the clicks on its left side,
            // top and bottom centre, even above the viewport.
            viewer.ShowAt(new FVector2D { X = -40, Y = -40 }, new FVector2D { X = 1, Y = 1 }, ScreenWidget.AboveGameUI);
            viewer.Refresh();
            Timer.Start(viewer, nameof(Refresh), 1f, loop: true);
            return viewer;
        }

        /// <summary>Removes the viewer from the screen.</summary>
        public void Close()
        {
            Timer.Stop(this, nameof(Refresh));
            Hide();
        }

        bool Build()
        {
            var tree = WidgetTree;
            if (tree == null)
            {
                tree = UGameplayStatics.SpawnObject(Unreal.ClassOf<UWidgetTree>(), this) as UWidgetTree;
                WidgetTree = tree;
            }
            var frame = UGameplayStatics.SpawnObject(Unreal.ClassOf<UBorder>(), tree) as UBorder;
            var column = UGameplayStatics.SpawnObject(Unreal.ClassOf<UVerticalBox>(), tree) as UVerticalBox;
            var buttons = UGameplayStatics.SpawnObject(Unreal.ClassOf<UHorizontalBox>(), tree) as UHorizontalBox;
            body = UGameplayStatics.SpawnObject(Unreal.ClassOf<UTextBlock>(), tree) as UTextBlock;
            if (tree == null || frame == null || column == null || buttons == null || body == null) return false;

            // The title on top, the buttons under the text: the bottom right corner is where clicks get through on the main menu.
            var title = Label(tree, $"NeoRune log: {Unreal.ModName}", 18);
            var copy = Button(tree, "Copy");
            var close = Button(tree, "Close");
            if (copy != null) { copy.OnClicked += Copy; buttons.AddChildToHorizontalBox(copy); }
            if (close != null) { close.OnClicked += Close; buttons.AddChildToHorizontalBox(close)?.SetPadding(new FMargin { Left = 8 }); }

            Style(body, 14);
            if (title != null) column.AddChildToVerticalBox(title);
            column.AddChildToVerticalBox(body)?.SetPadding(new FMargin { Top = 8 });
            var buttonsSlot = column.AddChildToVerticalBox(buttons);
            buttonsSlot?.SetPadding(new FMargin { Top = 8 });
            buttonsSlot?.SetHorizontalAlignment(EHorizontalAlignment.HAlign_Right);
            frame.SetBrushColor(new FLinearColor { R = 0, G = 0, B = 0, A = 0.8f });
            frame.SetPadding(new FMargin { Left = 12, Top = 8, Right = 12, Bottom = 8 });
            frame.AddChild(column);
            tree.RootWidget = frame;
            return true;
        }

        void Refresh()
        {
            var lines = Log.ReadLines();
            var text = "";
            for (int i = lines.Count > VisibleLines ? lines.Count - VisibleLines : 0; i < lines.Count; i++)
                text += (text.Length > 0 ? "\n" : "") + lines[i];
            body?.SetText(text.Length > 0 ? text : "(nothing logged yet)");
        }

        void Copy()
        {
            var text = "";
            foreach (var line in Log.ReadLines()) text += line + "\n";
            UMainMenuFunctionLibrary.CopyToClipboard(text);
        }

        static UTextBlock? Label(UObject outer, string text, int size)
        {
            var label = UGameplayStatics.SpawnObject(Unreal.ClassOf<UTextBlock>(), outer) as UTextBlock;
            if (label == null) return null;
            label.SetText(text);
            Style(label, size);
            return label;
        }

        static UButton? Button(UObject outer, string text)
        {
            var button = UGameplayStatics.SpawnObject(Unreal.ClassOf<UButton>(), outer) as UButton;
            var label = Label(outer, text, 14);
            if (button == null || label == null) return null;
            button.SetBackgroundColor(new FLinearColor { R = 0.15f, G = 0.15f, B = 0.15f, A = 1 });
            button.AddChild(label);
            return button;
        }

        /// <summary>The game's font (the engine's default font isn't always cooked into the game), in light grey.</summary>
        static void Style(UTextBlock text, int size)
        {
            var font = UMinimapHelpersLibrary.GetDefaultFont();
            if (font.FontObject == null) font = text.Font;   // the game's font isn't loaded yet: size the text block's own
            font.Size = size;
            text.SetFont(font);
            text.SetColorAndOpacity(new FSlateColor
            {
                SpecifiedColor = new FLinearColor { R = 0.9f, G = 0.9f, B = 0.9f, A = 1 },
                ColorUseRule = ESlateColorStylingMode.UseColor_Specified,
            });
        }
    }
}
