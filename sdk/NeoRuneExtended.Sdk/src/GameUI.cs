using System.Collections.Generic;
using UE.CommonGame;
using UE.CommonUI;
using UE.CoreUObject;
using UE.Engine;
using UE.GameplayTags;
using UE.SlateCore;
using UE.UIExtension;
using UE.UMG;
using UE.GameplayMessageRuntime;

namespace NeoRune
{
    /// <summary>
    /// The game's UI slots (UI extension points): places in its screens that take widgets from anyone. Put a widget into one
    /// with <see cref="GameUI.AddToSlot"/> and the game lays it out with its own content there. <see cref="GameUI.LogSlots"/>
    /// logs the slots that exist right now.
    /// </summary>
    public static class GameSlots
    {
        // The HUD, in levels.
        public const string QuestTracker = "SW.UI.Slot.HUD.QuestTracker";
        public const string MiniMap = "SW.UI.Slot.HUD.MiniMap";
        public const string Hotbar = "SW.UI.Slot.HUD.Hotbar";
        public const string Party = "SW.UI.Slot.HUD.Party";
        public const string LootTicker = "SW.UI.Slot.HUD.LootTicker";
        public const string EventLog = "SW.UI.Slot.HUD.EventLog";
        public const string BossHealthBars = "SW.UI.Slot.HUD.BossHealthBars";
        public const string SoulImbalance = "SW.UI.Slot.HUD.SoulImbalance";
        public const string SoulStormTracker = "SW.UI.Slot.HUD.SoulStormTracker";
        public const string TopTemporary = "SW.UI.Slot.HUD.TopTemporary";
        public const string TopTemporaryNotifications = "SW.UI.Slot.HUD.TopTemporaryNotifications";
        public const string SideTemporaryNotifications = "SW.UI.Slot.HUD.SideTemporaryNotifications";
        public const string BottomTemporaryNotifications = "SW.UI.Slot.HUD.BottomTemporaryNotifications";
        public const string SideIngameToast = "SW.UI.Slot.HUD.SideIngameToast";
        /// <summary>The whole screen, inside the safe zone (away from TV edges).</summary>
        public const string FullscreenWithinSafeZone = "SW.UI.Slot.HUD.FullscreenWithinSafeZone";
        /// <summary>The whole screen, edge to edge.</summary>
        public const string FullscreenOutsideSafeZone = "SW.UI.Slot.HUD.FullscreenOutsideSafeZone";

        // The activity layer: in levels and menus.
        public const string Toasts = "SW.UI.Slot.Activity.Toasts";
        public const string Notification = "SW.UI.Slot.Activity.Notification";
        public const string Throbber = "SW.UI.Slot.Activity.Throbber";

        /// <summary>The system (pause) menu.</summary>
        public const string SystemMenu = "SW.UI.Slot.System";
    }

    /// <summary>
    /// The layers of the game's screen, bottom to top. Each is a stack of screens: open yours with <see cref="GameUI.Open"/>
    /// on the layer the game uses for the same kind of screen.
    /// </summary>
    public static class GameLayers
    {
        /// <summary>The HUD.</summary>
        public const string InGame = "SW.UI.Layer.InGame";
        /// <summary>Screens opened in a level: inventory, map, collectibles. The layer for a mod's own menu in a level.</summary>
        public const string InGameMenu = "SW.UI.Layer.InGameMenu";
        public const string InGameOnboarding = "SW.UI.Layer.InGameOnboarding";
        /// <summary>Main menu screens and full-screen menus (settings).</summary>
        public const string Menu = "SW.UI.Layer.Menu";
        public const string Activity = "SW.UI.Layer.Activity";
        public const string ContextMenu = "SW.UI.Layer.ContextMenu";
        /// <summary>Dialogs, over everything else.</summary>
        public const string Modal = "SW.UI.Layer.Modal";
        public const string TopTemporary = "SW.UI.Layer.TopTemporary";
        /// <summary>The system (pause) menu.</summary>
        public const string System = "SW.UI.Layer.System";
        public const string Onboarding = "SW.UI.Layer.Onboarding";
        public const string Cutscene = "SW.UI.Layer.Cutscene";
    }

    /// <summary>
    /// The game's own UI: put widgets into its slots and into panels of its screens, and open menus on its layers.
    /// <code>
    /// GameUI.AddToSlot(this, GameSlots.QuestTracker, Unreal.ClassOf&lt;MyTracker&gt;(), 0);
    /// GameUI.AddToPanel(this, Unreal.ClassOf&lt;UAS_SomeScreen&gt;(), "ButtonList", Unreal.ClassOf&lt;MyButton&gt;(), -1);
    /// GameUI.Open(this, GameLayers.InGameMenu, Unreal.ClassOf&lt;MyMenu&gt;());
    /// </code>
    /// </summary>
    public static class GameUI
    {
        /// <summary>A gameplay tag by name, e.g. a slot from <see cref="GameSlots"/> or a layer from <see cref="GameLayers"/>.</summary>
        public static FGameplayTag Tag(string name) => new FGameplayTag { TagName = name };

        /// <summary>The game's slot registry for the current level.</summary>
        public static UUIExtensionSubsystem? Slots(UObject context) =>
            USubsystemBlueprintLibrary.GetWorldSubsystem(context, Unreal.ClassOf<UUIExtensionSubsystem>()) as UUIExtensionSubsystem;

        /// <summary>
        /// Puts a widget into one of the game's slots (<see cref="GameSlots"/>) until the level ends or
        /// <see cref="GameSlot.Remove"/>. The slot creates the widget from its class: a <see cref="ModWidget"/>, which builds its
        /// content when it's created. It fills the slot: align your content inside it. A higher priority puts it first.
        /// The widget goes in once the slot exists (the HUD's slots a moment into a level, the pause menu's when it opens):
        /// adding one while the game builds that screen can crash it.
        /// </summary>
        public static GameToast? Toast(UObject context, string text) => Toast(context, text, 3f);

        /// <summary>
        /// A short message in the game's toast area (its activity toasts), like "Settings saved". It goes away after <paramref name="seconds"/> (counted from when it shows); toasts shown
        /// together stack. Nothing runs once it's gone.
        /// </summary>
        public static GameToast? Toast(UObject context, string text, float seconds)
        {
            // The activity toasts: they show on the main menu and in levels (the HUD's side toasts didn't, checked in game).
            var slot = AddToSlot(context, GameSlots.Toasts, Unreal.ClassOf<ToastWidget>(), 0);
            var toast = World.Spawn(context, Unreal.ClassOf<GameToast>(), new FVector()) as GameToast;
            if (toast == null) { slot?.Remove(); return null; }
            toast.Start(slot, text, seconds);
            return toast;
        }

        public static GameSlot? AddToSlot(UObject context, string slot, TSubclassOf<ModWidget> widget, int priority)
        {
            var added = World.Spawn(context, Unreal.ClassOf<GameSlot>(), new FVector()) as GameSlot;
            added?.Start(slot, widget, priority);
            return added;
        }

        /// <summary>Logs the slots that exist right now (the ones of the screens on show), to find where yours can go.</summary>
        public static void LogSlots(UObject context)
        {
            var slots = Slots(context);
            if (slots == null) return;
            var lines = new List<string>();
            foreach (var tag in slots.DEBUGONLY_GetExtensionPoints()) lines.Add($"slot {tag.TagName}");
            Log.WriteAll(lines);
        }

        /// <summary>
        /// Opens a screen on one of the game's layers (<see cref="GameLayers"/>), like the game opens its own: the layer creates
        /// it from its class, shows it over the screens under it and routes input to it. Derive it from <see cref="ModMenu"/>,
        /// which builds its content and closes with Esc or the gamepad's B. Returns the screen, or null without a player.
        /// </summary>
        public static UCommonActivatableWidget? Open(UObject context, string layer, TSubclassOf<UCommonActivatableWidget> screen)
        {
            var player = UCommonUIExtensions.GetLocalPlayerFromController(World.PlayerController(context));
            if (player == null) return null;
            var step = $"GameUI.Open {layer}";
            Trail.Begin(step);
            var opened = UCommonUIExtensions.PushContentToLayer_ForPlayer(player, Tag(layer), screen);
            Trail.End(step);
            return opened;
        }

        /// <summary>Closes a screen opened with <see cref="Open"/> (or one of the game's).</summary>
        public static void Close(UCommonActivatableWidget? screen)
        {
            if (screen != null) UCommonUIExtensions.PopContentFromLayer(screen);
        }

        /// <summary>
        /// Adds a widget to a named panel of one of the game's screens each time that screen appears, at a position (0 first,
        /// -1 last), until the level ends. The widget is created from its class: derive it from <see cref="ModWidget"/>. The
        /// screen's widget names are the ones <see cref="Dump"/> logs.
        /// </summary>
        public static PanelInjector? AddToPanel(UObject context, TSubclassOf<UUserWidget> screen, string panel, TSubclassOf<UUserWidget> widget, int index)
        {
            var injector = World.Spawn(context, Unreal.ClassOf<PanelInjector>(), new FVector()) as PanelInjector;
            injector?.Start(screen, panel, widget, index);
            return injector;
        }

        /// <summary>The first visible widget of a class, e.g. one of the game's screens, or null.</summary>
        public static UUserWidget? FindOnScreen(UObject context, TSubclassOf<UUserWidget> widgetClass)
        {
            UWidgetBlueprintLibrary.GetAllWidgetsOfClass(context, out var widgets, widgetClass, false);
            foreach (var widget in widgets)
                if (widget != null && widget.IsVisible()) return widget;
            return null;
        }

        /// <summary>
        /// The widget called <paramref name="name"/> inside <paramref name="root"/>, also inside the widgets it's made of, or
        /// null. When a name is used more than once, search from a widget closer to the one you want.
        /// </summary>
        public static UWidget? Find(UUserWidget? root, string name)
        {
            if (root == null) return null;
            var pending = new List<UWidget>();
            pending.AddRange(Children(root));
            while (pending.Count > 0)
            {
                var widget = pending[0];
                pending.RemoveAt(0);
                if (widget == null) continue;
                if (UKismetSystemLibrary.GetObjectName(widget) == name) return widget;
                pending.AddRange(Children(widget));
            }
            return null;
        }

        /// <summary>The first widget of a class inside <paramref name="root"/> (e.g. a game button's text), or null.</summary>
        public static UWidget? FindOfClass(UUserWidget? root, TSubclassOf<UWidget> widgetClass)
        {
            if (root == null) return null;
            var pending = new List<UWidget>();
            pending.AddRange(Children(root));
            while (pending.Count > 0)
            {
                var widget = pending[0];
                pending.RemoveAt(0);
                if (widget == null) continue;
                if (UKismetMathLibrary.ClassIsChildOf(UGameplayStatics.GetObjectClass(widget), widgetClass)) return widget;
                pending.AddRange(Children(widget));
            }
            return null;
        }

        /// <summary>
        /// Adds a widget to one of the game's panels at a position: 0 first, -1 last. In vertical and horizontal boxes it gets
        /// the spacing of its neighbour, so it lines up with the game's own children. Other panels add it at the end.
        /// </summary>
        public static bool Insert(UPanelWidget? panel, UWidget? child, int index)
        {
            if (panel == null || child == null) return false;
            var vertical = panel as UVerticalBox;
            var horizontal = panel as UHorizontalBox;
            if (vertical == null && horizontal == null) { panel.AddChild(child); return true; }
            int count = panel.GetChildrenCount();
            if (index < 0 || index > count) index = count;
            // Take the children from the position on out, add the new one, put them back with their slot settings.
            var moved = new List<UWidget>();
            var slots = new List<UPanelSlot>();
            for (int i = index; i < count; i++)
            {
                var widget = panel.GetChildAt(i);
                if (widget == null) continue;
                moved.Add(widget);
                slots.Add(widget.Slot!);   // a child of a panel always has its slot
            }
            var neighbour = moved.Count > 0 ? slots[0] : count > 0 ? panel.GetChildAt(count - 1)?.Slot : null;
            var paddings = new List<FMargin>();
            var sizes = new List<FSlateChildSize>();
            var horizontals = new List<EHorizontalAlignment>();
            var verticals = new List<EVerticalAlignment>();
            foreach (var slot in slots) Remember(slot, paddings, sizes, horizontals, verticals);
            var like = new List<FMargin>();
            var likeSizes = new List<FSlateChildSize>();
            var likeH = new List<EHorizontalAlignment>();
            var likeV = new List<EVerticalAlignment>();
            Remember(neighbour, like, likeSizes, likeH, likeV);
            foreach (var widget in moved) panel.RemoveChild(widget);
            var added = panel.AddChild(child);
            if (like.Count > 0) Restore(added, like[0], likeSizes[0], likeH[0], likeV[0], false);
            for (int i = 0; i < moved.Count; i++) Restore(panel.AddChild(moved[i]), paddings[i], sizes[i], horizontals[i], verticals[i], true);
            return true;
        }

        /// <summary>Adds a widget next to one of the game's widgets in its panel: before it or after it.</summary>
        public static bool InsertNextTo(UWidget? sibling, UWidget? child, bool after)
        {
            var panel = sibling?.GetParent();
            if (panel == null || child == null) return false;
            int index = panel.GetChildIndex(sibling);
            return Insert(panel, child, after ? index + 1 : index);
        }

        static void Remember(UPanelSlot? slot, List<FMargin> paddings, List<FSlateChildSize> sizes, List<EHorizontalAlignment> horizontals, List<EVerticalAlignment> verticals)
        {
            var v = slot as UVerticalBoxSlot;
            var h = slot as UHorizontalBoxSlot;
            if (v != null) { paddings.Add(v.Padding); sizes.Add(v.Size); horizontals.Add(v.HorizontalAlignment); verticals.Add(v.VerticalAlignment); }
            else if (h != null) { paddings.Add(h.Padding); sizes.Add(h.Size); horizontals.Add(h.HorizontalAlignment); verticals.Add(h.VerticalAlignment); }
        }

        static void Restore(UPanelSlot? slot, FMargin padding, FSlateChildSize size, EHorizontalAlignment horizontal, EVerticalAlignment vertical, bool withSize)
        {
            var v = slot as UVerticalBoxSlot;
            var h = slot as UHorizontalBoxSlot;
            if (v != null)
            {
                v.SetPadding(padding); v.SetHorizontalAlignment(horizontal); v.SetVerticalAlignment(vertical);
                if (withSize) v.SetSize(size);
            }
            else if (h != null)
            {
                h.SetPadding(padding); h.SetHorizontalAlignment(horizontal); h.SetVerticalAlignment(vertical);
                if (withSize) h.SetSize(size);
            }
        }

        /// <summary>
        /// Logs a widget's tree: each widget's name and class, indented, into the widgets it's made of, down to
        /// <paramref name="depth"/> levels of user widgets. Use the names with <see cref="Find"/> and <see cref="AddToPanel"/>.
        /// </summary>
        public static void Dump(UUserWidget? root, int depth)
        {
            if (root == null) return;
            var lines = new List<string>();
            lines.Add($"{UKismetSystemLibrary.GetObjectName(root)} : {ClassPath(root)}");
            DumpChildren(root, "  ", 0, depth, lines);
            Log.WriteAll(lines);
        }

        static void DumpChildren(UWidget widget, string indent, int userDepth, int depth, List<string> lines)
        {
            foreach (var child in Children(widget))
            {
                if (child == null || lines.Count >= 2000) continue;
                bool user = child is UUserWidget;
                bool open = !user || userDepth < depth;
                lines.Add($"{indent}{UKismetSystemLibrary.GetObjectName(child)} : {ClassPath(child)}{(child.IsVisible() ? "" : " [hidden]")}{(open ? "" : " [...]")}");
                if (open) DumpChildren(child, indent + "  ", user ? userDepth + 1 : userDepth, depth, lines);
            }
        }

        /// <summary>A widget's children: a panel's, a user widget's root, or the entries an entry box made.</summary>
        static List<UWidget> Children(UWidget widget)
        {
            var children = new List<UWidget>();
            var panel = widget as UPanelWidget;
            var user = widget as UUserWidget;
            var entries = widget as UDynamicEntryBoxBase;
            if (panel != null) children = panel.GetAllChildren();
            else if (user != null && user.WidgetTree?.RootWidget != null) children.Add(user.WidgetTree.RootWidget);
            else if (entries != null) foreach (var entry in entries.GetAllEntries()) children.Add(entry);
            return children;
        }

        /// <summary>The path of an object's class, e.g. /Game/Spicewood/UI/HUD/W_PlayerHUD.W_PlayerHUD_C.</summary>
        public static string ClassPath(UObject? obj) =>
            obj == null ? "" : UKismetSystemLibrary.Conv_SoftClassReferenceToString(UKismetSystemLibrary.Conv_ClassToSoftClassReference(UGameplayStatics.GetObjectClass(obj)));
    }

    /// <summary>Made by <see cref="GameUI.AddToSlot"/>: a widget in one of the game's slots.</summary>
    /// <summary>Made by <see cref="GameUI.Toast(UObject, string, float)"/>: a toast on its way, on screen, then gone.</summary>
    public class GameToast : AActor
    {
        GameSlot? slot;
        string text;
        float seconds;
        bool shown;
        double ends;
        double giveUp;
        /// <summary>Made while a level loads: its time starts when the loading screen goes (World.Transition.End).</summary>
        bool waitingForLevel;
        double waitUntil;
        UAsyncAction_ListenForGameplayMessage? transition;

        public void Start(GameSlot? toastSlot, string message, float duration)
        {
            slot = toastSlot;
            text = message;
            seconds = duration;
            // In a level's first seconds the loading screen may still cover the toast (found in game).
            if (!World.LevelName(this).StartsWith("Menu") && UGameplayStatics.GetTimeSeconds(this) < 15)
            {
                waitingForLevel = true;
                waitUntil = UGameplayStatics.GetTimeSeconds(this) + 15;
                transition = GameEvents.Listen(this, GameChannels.WorldTransitionEnd, null, false);
                if (transition != null) transition.OnMessageReceived += OnLevelShown;
            }
            // Toast areas appear a moment into a level: wait for it, but not forever.
            giveUp = UGameplayStatics.GetTimeSeconds(this) + 30;
            Timer.Start(this, nameof(Update), 0.1f, loop: true);
        }

        /// <summary>Takes the toast away now.</summary>
        public void Remove()
        {
            Timer.Stop(this, nameof(Update));
            slot?.Remove();
            K2_DestroyActor();
        }

        void OnLevelShown(UAsyncAction_ListenForGameplayMessage? message, FGameplayTag channel)
        {
            waitingForLevel = false;
            GameEvents.Stop(transition);
        }

        void Update()
        {
            // Game time: a toast made while a level loads isn't used up behind the loading screen.
            var now = UGameplayStatics.GetTimeSeconds(this);
            // The slot makes the widget from its class (also again if the game rebuilds the screen): give it the text.
            if (waitingForLevel && now >= waitUntil) waitingForLevel = false;
            if (slot?.Widget is ToastWidget widget)
            {
                widget.Show(text);
                if (!shown && !waitingForLevel) { shown = true; ends = now + seconds; Sounds.Play(this, Sounds.ToastTag); }
            }
            if (shown ? now >= ends : now >= giveUp) Remove();
        }
    }

    /// <summary>The look of <see cref="GameUI.Toast(UObject, string, float)"/>: the game's font on a dark panel.</summary>
    public class ToastWidget : ModWidget
    {
        UTextBlock? label;

        protected override UWidget? Build(UWidgetTree tree)
        {
            // The game's font at the size of its toasts; long messages wrap.
            label = Ui.Text(tree, "", 20);
            label?.SetAutoWrapText(true);
            var panel = Ui.Panel(tree, label, Ui.Color(0.02f, 0.02f, 0.03f, 0.85f), 12);
            // Off the screen's edge (the toast area starts right at it).
            panel = Ui.Panel(tree, panel, Ui.Color(0, 0, 0, 0), 12);
            var box = UGameplayStatics.SpawnObject(Unreal.ClassOf<USizeBox>(), tree) as USizeBox;
            if (box == null || panel == null) return panel;
            box.SetMaxDesiredWidth(480);
            box.AddChild(panel);
            return box;
        }

        public void Show(string text) => label?.SetText(text);
    }

    public class GameSlot : AActor
    {
        string slotName;
        TSubclassOf<ModWidget> widgetClass;
        int priority;
        bool registered;
        FUIExtensionHandle handle;

        public void Start(string slot, TSubclassOf<ModWidget> widget, int order)
        {
            slotName = slot;
            widgetClass = widget;
            priority = order;
            Timer.Start(this, nameof(Check), 0.25f, loop: true);
            Check();
        }

        /// <summary>The widget the slot made, or null while the slot's screen doesn't exist (or before the widget went in).</summary>
        public UUserWidget? Widget => registered ? GameUI.Slots(this)?.GetWidgetFromHandle(handle) : null;

        /// <summary>Takes the widget out of the slot.</summary>
        public void Remove()
        {
            Timer.Stop(this, nameof(Check));
            if (registered) GameUI.Slots(this)?.UnregisterExtension(handle);
            K2_DestroyActor();
        }

        /// <summary>Registers the widget once the slot exists; until then the screen isn't built yet.</summary>
        void Check()
        {
            var slots = GameUI.Slots(this);
            if (slots == null) return;
            foreach (var tag in slots.DEBUGONLY_GetExtensionPoints())
            {
                if (tag.TagName != slotName) continue;
                var step = $"GameUI.AddToSlot {slotName}";
                Trail.Begin(step);
                handle = slots.K2_RegisterExtensionAsWidget(GameUI.Tag(slotName), widgetClass, priority);
                Trail.End(step);
                registered = true;
                Timer.Stop(this, nameof(Check));
                return;
            }
        }
    }

    /// <summary>Made by <see cref="GameUI.AddToPanel"/>: adds a widget to a panel of each new screen of a class.</summary>
    public class PanelInjector : AActor
    {
        WidgetWatcher? watcher;
        string panelName;
        TSubclassOf<UUserWidget> widgetClass;
        int index;

        public void Start(TSubclassOf<UUserWidget> screen, string panel, TSubclassOf<UUserWidget> widget, int position)
        {
            panelName = panel;
            widgetClass = widget;
            index = position;
            watcher = WidgetWatcher.Start(this, screen, 0.25f);
            if (watcher != null) watcher.Found += OnScreen;
        }

        /// <summary>Stops adding the widget to new screens.</summary>
        public void Stop()
        {
            watcher?.Stop();
            K2_DestroyActor();
        }

        void OnScreen(UUserWidget screen)
        {
            var panel = GameUI.Find(screen, panelName) as UPanelWidget;
            if (panel == null) { Log.Write($"AddToPanel: no panel called {panelName} in {GameUI.ClassPath(screen)}"); return; }
            var step = $"GameUI.AddToPanel {panelName}";
            Trail.Begin(step);
            GameUI.Insert(panel, UWidgetBlueprintLibrary.Create(this, widgetClass, World.PlayerController(this)), index);
            Trail.End(step);
        }
    }
}
