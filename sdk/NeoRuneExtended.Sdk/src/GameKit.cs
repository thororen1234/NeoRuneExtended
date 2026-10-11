using System.Collections.Generic;
using UE.Angelscript;
using UE.CommonUI;
using UE.CoreUObject;
using UE.Engine;
using UE.OreUI;
using UE.UMG;

namespace NeoRune
{
    /// <summary>
    /// Widgets that look exactly like the game's own: they are new copies of the game's widget classes, taken from a widget
    /// of that kind that is on screen (the main menu, the pause menu, the inventory...). No asset paths are written in your
    /// mod, so they keep working after game updates. Each helper returns null when no such game widget is around: fall back
    /// to <see cref="Ui"/> then.
    /// <code>
    /// var button = GameKit.Button(this, "Open map");
    /// if (button != null) { button.OnButtonBaseClicked += OnOpen; panel.AddChild(button); }
    /// else { /* Ui.Button(...) */ }
    /// </code>
    /// Copies are Trail steps ("neorunex crash" names one that crashed).
    /// </summary>
    public static class GameKit
    {
        /// <summary>
        /// A new widget of the same Blueprint class as a game widget on screen that derives from <paramref name="codeClass"/>
        /// (e.g. UAS_SpicewoodButtonSingleAction, the game's single-action button). Only the game's own widgets are copied,
        /// never another mod's. Null when there is none.
        /// </summary>
        public static UUserWidget? Copy(UObject context, TSubclassOf<UUserWidget> codeClass)
        {
            var source = FindGameWidget(context, codeClass);
            if (source == null) return null;
            var path = GameUI.ClassPath(source);
            var widgetClass = Unreal.LoadClass<UUserWidget>(path);
            if (widgetClass == null) return null;
            Trail.Begin("GameKit.Copy " + path);
            var copy = UWidgetBlueprintLibrary.Create(context, widgetClass, World.PlayerController(context));
            Trail.End("GameKit.Copy " + path);
            return copy;
        }

        /// <summary>
        /// A button like the game's menu buttons, with a label. Subscribe to its OnButtonBaseClicked (a method taking the
        /// button). Null when no game button is on screen.
        /// </summary>
        public static UCommonButtonBase? Button(UObject context, string label)
        {
            var source = FindTextButton(context);
            if (source == null) return null;
            var path = GameUI.ClassPath(source);
            var widgetClass = Unreal.LoadClass<UUserWidget>(path);
            if (widgetClass == null) return null;
            Trail.Begin("GameKit.Button " + path);
            var button = UWidgetBlueprintLibrary.Create(context, widgetClass, World.PlayerController(context)) as UCommonButtonBase;
            Trail.End("GameKit.Button " + path);
            if (button == null) return null;
            // The game's buttons (OreUI) apply their ButtonText when they're shown, so the label goes there, marked as set
            // (found in game: only the text block's text was replaced by "Lobby Button"). Other buttons: their first text block.
            if (button is UOreUIButtonBase ore)
            {
                ore.bOverride_ButtonText = true;
                ore.ButtonText = label;
                ore.SetButtonText(label);
            }
            else if (GameUI.FindOfClass(button, Unreal.ClassOf<UTextBlock>()) is UTextBlock text) text.SetText(label);
            return button;
        }

        /// <summary>
        /// Text in the game's own text style: the style the game's buttons use (font, size, colour, shadow). Null when no game
        /// button is on screen to take the style from.
        /// </summary>
        public static UCommonTextBlock? Text(UObject context, string text)
        {
            var source = FindTextButton(context);
            var style = source?.GetCurrentTextStyleClass();
            if (style == null) return null;
            var block = UGameplayStatics.SpawnObject(Unreal.ClassOf<UCommonTextBlock>(), context) as UCommonTextBlock;
            if (block == null) return null;
            block.SetStyle(style);
            block.SetText(text);
            return block;
        }

        /// <summary>Logs the game's widget classes on screen that the kit can copy (button, list entry and tab classes).</summary>
        public static void LogAvailable(UObject context)
        {
            UWidgetBlueprintLibrary.GetAllWidgetsOfClass(context, out var widgets, Unreal.ClassOf<UCommonButtonBase>(), false);
            var seen = new HashSet<string>();
            foreach (var widget in widgets)
            {
                var path = GameUI.ClassPath(widget);
                if (!IsGameClass(path) || !seen.Add(path)) continue;
                Log.Write($"GameKit: {path}");
            }
            if (seen.Count == 0) Log.Write("GameKit: no game buttons on screen right now");
        }

        /// <summary>A Blueprint class of the game (its content or plugins, like /Game/... and /OreUI/...), not a mod's.</summary>
        static bool IsGameClass(string path) => path.Length > 0 && !path.StartsWith("/Game/Mods/") && !path.StartsWith("/Script/");

        static UUserWidget? FindGameWidget(UObject context, TSubclassOf<UUserWidget> codeClass)
        {
            UWidgetBlueprintLibrary.GetAllWidgetsOfClass(context, out var widgets, codeClass, false);
            foreach (var widget in widgets)
                if (IsGameClass(GameUI.ClassPath(widget))) return widget;
            return null;
        }

        /// <summary>
        /// A game button with a text label: the game's text buttons (".../Button/Role/Text/...", like the main menu's) first,
        /// then any other with a text block in it. Invisible and tab buttons are skipped.
        /// </summary>
        public static UCommonButtonBase? FindTextButton(UObject context)
        {
            UWidgetBlueprintLibrary.GetAllWidgetsOfClass(context, out var widgets, Unreal.ClassOf<UCommonButtonBase>(), false);
            UCommonButtonBase? fallback = null;
            foreach (var widget in widgets)
            {
                var path = GameUI.ClassPath(widget);
                if (!IsGameClass(path) || path.Contains("Invisible") || path.Contains("Tab")) continue;
                if (widget is not UCommonButtonBase button || GameUI.FindOfClass(button, Unreal.ClassOf<UTextBlock>()) == null) continue;
                if (path.Contains("/Role/Text/")) return button;
                if (fallback == null) fallback = button;
            }
            return fallback;
        }
    }
}
