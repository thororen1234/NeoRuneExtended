using System.Collections.Generic;
using UE.CommonGame;
using UE.CommonUI;
using UE.CoreUObject;
using UE.Engine;
using UE.GameplayTags;
using UE.SlateCore;
using UE.UIExtension;
using UE.UMG;

namespace NeoRune
{
    /// <summary>
    /// The game's UI slots (UI extension points): places in its screens that take widgets from anyone. Put yours into one
    /// with <see cref="GameUI.AddToSlot"/> and the game lays it out together with its own content there.
    /// </summary>
    public static class GameSlots
    {
        // In the HUD (W_PlayerHUD), in game.
        public const string QuestTracker = "SW.UI.Slot.HUD.QuestTracker";
        public const string MiniMap = "SW.UI.Slot.HUD.MiniMap";
        public const string Party = "SW.UI.Slot.HUD.Party";
        public const string LootTicker = "SW.UI.Slot.HUD.LootTicker";
        public const string EventLog = "SW.UI.Slot.HUD.EventLog";
        public const string BossHealthBars = "SW.UI.Slot.HUD.BossHealthBars";
        public const string SoulImbalance = "SW.UI.Slot.HUD.SoulImbalance";
        public const string SoulStormTracker = "SW.UI.Slot.HUD.SoulStormTracker";
        public const string TopTemporary = "SW.UI.Slot.HUD.TopTemporary";
        public const string SideIngameToast = "SW.UI.Slot.HUD.SideIngameToast";
        public const string SideTemporaryNotifications = "SW.UI.Slot.HUD.SideTemporaryNotifications";
        public const string BottomTemporaryNotifications = "SW.UI.Slot.HUD.BottomTemporaryNotifications";
        /// <summary>The whole screen, inside the safe zone (away from TV edges).</summary>
        public const string FullscreenWithinSafeZone = "SW.UI.Slot.HUD.FullscreenWithinSafeZone";
        public const string FullscreenOutsideSafeZone = "SW.UI.Slot.HUD.FullscreenOutsideSafeZone";

        // On the activity layer (W_ActivityLayer): in game and in menus.
        public const string Toasts = "SW.UI.Slot.Activity.Toasts";
        public const string Notification = "SW.UI.Slot.Activity.Notification";
        public const string Throbber = "SW.UI.Slot.Activity.Throbber";

        /// <summary>In the system (pause) menu, beside its buttons.</summary>
        public const string SystemMenu = "SW.UI.Slot.System";
    }

    /// <summary>
    /// The layers of the game's screen (W_OverallUILayout), bottom to top. Each is a stack of screens: push yours with
    /// <see cref="GameUI.Push"/> onto the layer the game uses for the same kind of screen.
    /// </summary>
    public static class GameLayers
    {
        /// <summary>The HUD.</summary>
        public const string InGame = "SW.UI.Layer.InGame";
        /// <summary>Screens opened in game: inventory, map, collectibles. Use it for a mod's own menu in game.</summary>
        public const string InGameMenu = "SW.UI.Layer.InGameMenu";
        public const string InGameOnboarding = "SW.UI.Layer.InGameOnboarding";
        /// <summary>Main menu screens and full-screen menus (settings).</summary>
        public const string Menu = "SW.UI.Layer.Menu";
        public const string Activity = "SW.UI.Layer.Activity";
        public const string ContextMenu = "SW.UI.Layer.ContextMenu";
        /// <summary>Dialogs over everything else.</summary>
        public const string Modal = "SW.UI.Layer.Modal";
        public const string TopTemporary = "SW.UI.Layer.TopTemporary";
        /// <summary>The system (pause) menu.</summary>
        public const string System = "SW.UI.Layer.System";
        public const string Onboarding = "SW.UI.Layer.Onboarding";
        public const string Cutscene = "SW.UI.Layer.Cutscene";
    }

    /// <summary>
    /// Working with the game's own UI: put widgets into its slots and panels, and push menus onto its layers.
    /// <code>
    /// GameUI.AddToSlot(this, GameSlots.QuestTracker, Unreal.ClassOf&lt;MyTracker&gt;(), 0);
    /// GameUI.Push(this, GameLayers.InGameMenu, Unreal.ClassOf&lt;MyMenu&gt;());
    /// </code>
    /// </summary>
    public static class GameUI
    {
        /// <summary>A gameplay tag by name, e.g. a slot or layer from <see cref="GameSlots"/> or <see cref="GameLayers"/>.</summary>
        public static FGameplayTag Tag(string name) => new FGameplayTag { TagName = name };

        /// <summary>The game's UI slot registry for the current level.</summary>
        public static UUIExtensionSubsystem? Slots(UObject context) =>
            USubsystemBlueprintLibrary.GetWorldSubsystem(context, Unreal.ClassOf<UUIExtensionSubsystem>()) as UUIExtensionSubsystem;

        /// <summary>
        /// Puts a widget into a slot of the game's screens (<see cref="GameSlots"/>), now and whenever that screen is made
        /// again, until the level ends or <see cref="RemoveFromSlot"/>. The slot creates the widget from its class, so build
        /// its content when it's created: derive it from <see cref="ModWidget"/>. Widgets with a higher priority come first.
        /// </summary>
        public static FUIExtensionHandle AddToSlot(UObject context, string slot, TSubclassOf<UUserWidget> widget, int priority)
        {
            var slots = Slots(context);
            if (slots == null) return new FUIExtensionHandle();
            return slots.K2_RegisterExtensionAsWidget(Tag(slot), widget, priority);
        }

        /// <summary>Takes a widget <see cref="AddToSlot"/> put into a slot out again.</summary>
        public static void RemoveFromSlot(UObject context, FUIExtensionHandle handle)
        {
            var slots = Slots(context);
            if (slots != null) slots.UnregisterExtension(handle);
        }

        /// <summary>The widget a slot made for <see cref="AddToSlot"/> (null while the slot's screen doesn't exist).</summary>
        public static UUserWidget? SlotWidget(UObject context, FUIExtensionHandle handle)
        {
            var slots = Slots(context);
            if (slots == null) return null;
            return slots.GetWidgetFromHandle(handle);
        }

        /// <summary>
        /// Opens a screen on one of the game's layers (<see cref="GameLayers"/>), like the game opens its own: the layer
        /// creates it from its class, shows it over the screens under it, and routes input to it. Derive it from
        /// <see cref="ModMenu"/>, which builds its content and closes with Esc / B. Returns the screen, or null without a player.
        /// </summary>
        public static UCommonActivatableWidget? Push(UObject context, string layer, TSubclassOf<UCommonActivatableWidget> screen)
        {
            var player = UCommonUIExtensions.GetLocalPlayerFromController(World.PlayerController(context));
            if (player == null) return null;
            return UCommonUIExtensions.PushContentToLayer_ForPlayer(player, Tag(layer), screen);
        }

        /// <summary>Closes a screen opened with <see cref="Push"/> (or one of the game's), taking it off its layer.</summary>
        public static void Close(UCommonActivatableWidget? screen)
        {
            if (screen != null) UCommonUIExtensions.PopContentFromLayer(screen);
        }

        /// <summary>
        /// The widget called <paramref name="name"/> inside <paramref name="root"/>, including inside the widgets it is made
        /// of, or null. Names are the ones in the game's widget blueprints: <see cref="Dump"/> lists them. When a name is used
        /// more than once, search from a widget closer to the one you want.
        /// </summary>
        public static UWidget? Find(UUserWidget? root, string name)
        {
            if (root == null) return null;
            var pending = new List<UWidget>();
            pending.Add(root);
            while (pending.Count > 0)
            {
                var widget = pending[pending.Count - 1];
                pending.RemoveAt(pending.Count - 1);
                if (widget == null) continue;
                if (widget != root && UKismetSystemLibrary.GetObjectName(widget) == name) return widget;
                // Pushed in reverse, so children are searched first to last.
                var children = Children(widget);
                for (int i = children.Count - 1; i >= 0; i--) pending.Add(children[i]);
            }
            return null;
        }

        /// <summary>
        /// The first widget of a class inside <paramref name="root"/>, including inside the widgets it is made of, or null:
        /// e.g. the text block of one of the game's buttons.
        /// </summary>
        public static UWidget? FindOfClass(UUserWidget? root, TSubclassOf<UWidget> widgetClass)
        {
            if (root == null) return null;
            var pending = new List<UWidget>();
            pending.Add(root);
            while (pending.Count > 0)
            {
                var widget = pending[pending.Count - 1];
                pending.RemoveAt(pending.Count - 1);
                if (widget == null) continue;
                if (widget != root && UKismetMathLibrary.ClassIsChildOf(UGameplayStatics.GetObjectClass(widget), widgetClass)) return widget;
                var children = Children(widget);
                for (int i = children.Count - 1; i >= 0; i--) pending.Add(children[i]);
            }
            return null;
        }

        /// <summary>The first widget of a class on screen, e.g. one of the game's screens, or null.</summary>
        public static UUserWidget? FindOnScreen(UObject context, TSubclassOf<UUserWidget> widgetClass)
        {
            UWidgetBlueprintLibrary.GetAllWidgetsOfClass(context, out var widgets, widgetClass, false);
            foreach (var widget in widgets)
                if (widget != null && widget.IsVisible()) return widget;
            return null;
        }

        /// <summary>
        /// Adds a widget to one of the game's panels at a position: 0 first, -1 (or past the end) last. In vertical and
        /// horizontal boxes the widget gets the padding and alignment of the child it's placed before (or after, at the end),
        /// so it's spaced like the game's own, and the children after it keep theirs. Other panels add it at the end.
        /// </summary>
        public static bool Insert(UPanelWidget? panel, UWidget? child, int index)
        {
            if (panel == null || child == null) return false;
            if (panel is UVerticalBox vertical) InsertVertical(vertical, child, index);
            else if (panel is UHorizontalBox horizontal) InsertHorizontal(horizontal, child, index);
            else panel.AddChild(child);
            return true;
        }

        /// <summary>
        /// Adds a widget next to one of the game's widgets in its parent panel: before it, or after it. Use
        /// <see cref="Find"/> to get the widget, e.g. a button in the system menu.
        /// </summary>
        public static bool InsertNextTo(UWidget? sibling, UWidget? child, bool after)
        {
            if (sibling == null || child == null) return false;
            var panel = sibling.GetParent();
            if (panel == null) return false;
            var index = panel.GetChildIndex(sibling);
            return Insert(panel, child, after ? index + 1 : index);
        }

        static void InsertVertical(UVerticalBox box, UWidget child, int index)
        {
            var count = box.GetChildrenCount();
            if (index < 0 || index > count) index = count;
            // The children from index on are taken out and put back after the new one, with their slots' settings.
            var moved = new List<UWidget>();
            var paddings = new List<FMargin>();
            var sizes = new List<FSlateChildSize>();
            var horizontals = new List<EHorizontalAlignment>();
            var verticals = new List<EVerticalAlignment>();
            for (int i = index; i < count; i++)
            {
                var widget = box.GetChildAt(i);
                var slot = widget?.Slot as UVerticalBoxSlot;
                if (widget == null || slot == null) continue;
                moved.Add(widget);
                paddings.Add(slot.Padding);
                sizes.Add(slot.Size);
                horizontals.Add(slot.HorizontalAlignment);
                verticals.Add(slot.VerticalAlignment);
            }
            // Spaced like its neighbour: the child it goes before, or the last one.
            var like = moved.Count > 0 ? moved[0] : (count > 0 ? box.GetChildAt(count - 1) : null);
            var likeSlot = like?.Slot as UVerticalBoxSlot;
            foreach (var widget in moved) box.RemoveChild(widget);
            var added = box.AddChildToVerticalBox(child);
            if (added != null && likeSlot != null)
            {
                added.SetPadding(moved.Count > 0 ? paddings[0] : likeSlot.Padding);
                added.SetHorizontalAlignment(moved.Count > 0 ? horizontals[0] : likeSlot.HorizontalAlignment);
                added.SetVerticalAlignment(moved.Count > 0 ? verticals[0] : likeSlot.VerticalAlignment);
            }
            for (int i = 0; i < moved.Count; i++)
            {
                var slot = box.AddChildToVerticalBox(moved[i]);
                if (slot == null) continue;
                slot.SetPadding(paddings[i]);
                slot.SetSize(sizes[i]);
                slot.SetHorizontalAlignment(horizontals[i]);
                slot.SetVerticalAlignment(verticals[i]);
            }
        }

        static void InsertHorizontal(UHorizontalBox box, UWidget child, int index)
        {
            var count = box.GetChildrenCount();
            if (index < 0 || index > count) index = count;
            var moved = new List<UWidget>();
            var paddings = new List<FMargin>();
            var sizes = new List<FSlateChildSize>();
            var horizontals = new List<EHorizontalAlignment>();
            var verticals = new List<EVerticalAlignment>();
            for (int i = index; i < count; i++)
            {
                var widget = box.GetChildAt(i);
                var slot = widget?.Slot as UHorizontalBoxSlot;
                if (widget == null || slot == null) continue;
                moved.Add(widget);
                paddings.Add(slot.Padding);
                sizes.Add(slot.Size);
                horizontals.Add(slot.HorizontalAlignment);
                verticals.Add(slot.VerticalAlignment);
            }
            var like = moved.Count > 0 ? moved[0] : (count > 0 ? box.GetChildAt(count - 1) : null);
            var likeSlot = like?.Slot as UHorizontalBoxSlot;
            foreach (var widget in moved) box.RemoveChild(widget);
            var added = box.AddChildToHorizontalBox(child);
            if (added != null && likeSlot != null)
            {
                added.SetPadding(moved.Count > 0 ? paddings[0] : likeSlot.Padding);
                added.SetHorizontalAlignment(moved.Count > 0 ? horizontals[0] : likeSlot.HorizontalAlignment);
                added.SetVerticalAlignment(moved.Count > 0 ? verticals[0] : likeSlot.VerticalAlignment);
            }
            for (int i = 0; i < moved.Count; i++)
            {
                var slot = box.AddChildToHorizontalBox(moved[i]);
                if (slot == null) continue;
                slot.SetPadding(paddings[i]);
                slot.SetSize(sizes[i]);
                slot.SetHorizontalAlignment(horizontals[i]);
                slot.SetVerticalAlignment(verticals[i]);
            }
        }

        /// <summary>
        /// Logs a widget's tree: each widget's name and class, indented, into the widgets it's made of, up to
        /// <paramref name="depth"/> levels of user widgets. Use the names with <see cref="Find"/>.
        /// </summary>
        public static void Dump(UUserWidget? root, int depth)
        {
            if (root == null) return;
            var lines = new List<string>();
            lines.Add($"{UKismetSystemLibrary.GetObjectName(root)} : {ClassPath(root)}");
            var stack = new List<UWidget>();
            var indents = new List<string>();
            var userDepths = new List<int>();
            var children = Children(root);
            for (int i = children.Count - 1; i >= 0; i--)
            {
                stack.Add(children[i]);
                indents.Add("  ");
                userDepths.Add(0);
            }
            while (stack.Count > 0 && lines.Count < 2000)
            {
                var last = stack.Count - 1;
                var widget = stack[last];
                var indent = indents[last];
                var userDepth = userDepths[last];
                stack.RemoveAt(last);
                indents.RemoveAt(last);
                userDepths.RemoveAt(last);
                if (widget == null) continue;
                var isUser = widget is UUserWidget;
                var open = !isUser || userDepth < depth;
                lines.Add($"{indent}{UKismetSystemLibrary.GetObjectName(widget)} : {ClassPath(widget)}{(widget.IsVisible() ? "" : " [hidden]")}{(open ? "" : " [...]")}");
                if (!open) continue;
                var inner = Children(widget);
                for (int i = inner.Count - 1; i >= 0; i--)
                {
                    stack.Add(inner[i]);
                    indents.Add(indent + "  ");
                    userDepths.Add(isUser ? userDepth + 1 : userDepth);
                }
            }
            Log.WriteAll(lines);
        }

        /// <summary>A widget's children: a panel's, a user widget's root, or the widgets a slot or entry box made.</summary>
        static List<UWidget> Children(UWidget widget)
        {
            var children = new List<UWidget>();
            if (widget is UPanelWidget panel) children = panel.GetAllChildren();
            else if (widget is UUserWidget user)
            {
                var tree = user.WidgetTree;
                if (tree != null && tree.RootWidget != null) children.Add(tree.RootWidget);
            }
            else if (widget is UDynamicEntryBoxBase entries)
            {
                foreach (var entry in entries.GetAllEntries()) children.Add(entry);
            }
            return children;
        }

        /// <summary>The path of an object's class, e.g. /Game/Spicewood/UI/HUD/W_PlayerHUD.W_PlayerHUD_C.</summary>
        public static string ClassPath(UObject? obj) =>
            obj == null ? "" : UKismetSystemLibrary.Conv_SoftClassReferenceToString(UKismetSystemLibrary.Conv_ClassToSoftClassReference(UGameplayStatics.GetObjectClass(obj)));
    }
}
