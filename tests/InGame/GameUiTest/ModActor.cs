using NeoRune;
using UE.Angelscript;
using UE.CommonUI;
using UE.CoreUObject;
using UE.Engine;
using UE.InputCore;
using UE.UMG;

namespace GameUiTest;

/// <summary>
/// In-game test of NeoRuneExtended's game UI features. Read the results with: neorunex log GameUiTest
/// - Main menu, once per install: calls the lobby's AngelScript OnOpenSettingsInput() as a plain C# call. The settings
///   screen opening means AngelScript calls without parameters work.
/// - System menu (Esc in game): a copy of the game's own Settings button, labelled "Mods (test)", appears under Settings.
///   Clicking it calls the system menu's AngelScript OnOpenSettingsClicked(button): settings opening means calls with
///   parameters work.
/// - F6: puts a TestSlot label into the quest tracker and system menu slots.
/// - F7: opens TestMenu on the game's menu layer: the mouse should work, and Esc and its Close button close it.
/// </summary>
public class ModActor : AActor
{
    const string SystemMenuClass = "/SpicewoodSettings/SystemMenu/W_SystemMenu.W_SystemMenu_C";

    WidgetWatcher? systemMenus;
    UAS_SystemMenu? systemMenu;
    UCommonButtonBase? settingsButton;
    UCommonButtonBase? modsButton;
    PausableTimer? labelTimer;
    PausableTimer? lobbyTimer;

    protected override void ReceiveBeginPlay()
    {
        SetTickableWhenPaused(true);
        Log.Write($"GameUiTest in {World.LevelName(this)}: F6 slots, F7 menu, Esc for the system menu button");
        var systemMenuClass = Unreal.LoadClass<UUserWidget>(SystemMenuClass);
        if (systemMenuClass == null) Log.Write("System menu class not found: " + SystemMenuClass);
        else
        {
            systemMenus = WidgetWatcher.Start(this, systemMenuClass, 0.2f);
            if (systemMenus != null) systemMenus.Found += OnSystemMenu;
        }
        lobbyTimer = PausableTimer.Start(this, 1f, loop: true);
        if (lobbyTimer != null) lobbyTimer.Fired += LobbyTestOnce;
    }

    public override void ReceiveTick(float deltaSeconds)
    {
        var controller = World.PlayerController(this);
        if (controller == null) return;
        if (controller.WasInputKeyJustPressed(new FKey { KeyName = "F6" })) FillSlots();
        if (controller.WasInputKeyJustPressed(new FKey { KeyName = "F7" })) OpenMenu();
    }

    void LobbyTestOnce()
    {
        var lobby = GameUI.FindOnScreen(this, Unreal.ClassOf<UAS_InitialLobbyScreen>()) as UAS_InitialLobbyScreen;
        if (lobby == null) return;
        lobbyTimer?.Stop();
        var save = UGameplayStatics.LoadGameFromSlot("GameUiTest", 0) as TestSave
                   ?? UGameplayStatics.CreateSaveGameObject(Unreal.ClassOf<TestSave>()) as TestSave;
        if (save == null || save.LobbyTestDone) return;
        save.LobbyTestDone = true;
        UGameplayStatics.SaveGameToSlot(save, "GameUiTest", 0);
        Log.Write("Calling the lobby's AngelScript OnOpenSettingsInput(): settings should open");
        lobby.OnOpenSettingsInput();
        Log.Write("Back from OnOpenSettingsInput");
    }

    void OnSystemMenu(UUserWidget widget)
    {
        systemMenu = widget as UAS_SystemMenu;
        settingsButton = GameUI.Find(widget, "OpenSettings") as UCommonButtonBase;
        Log.Write($"System menu {GameUI.ClassPath(widget)} (AngelScript class: {systemMenu != null}); Settings button {GameUI.ClassPath(settingsButton)}");
        if (settingsButton == null) return;
        GameUI.Dump(settingsButton, 3);
        // A copy of the game's button: same class, so the same look.
        modsButton = Ui.GameWidget(this, GameUI.ClassPath(settingsButton)) as UCommonButtonBase;
        if (modsButton == null)
        {
            Log.Write("Couldn't create the game's button");
            return;
        }
        modsButton.OnButtonBaseClicked += OnModsClicked;
        Log.Write($"Inserted under Settings: {GameUI.InsertNextTo(settingsButton, modsButton, true)}");
        // Its label is set when it's constructed on screen: change it after that.
        labelTimer = PausableTimer.Start(this, 0.1f, loop: false);
        if (labelTimer != null) labelTimer.Fired += LabelModsButton;
    }

    void LabelModsButton()
    {
        var text = GameUI.FindOfClass(modsButton, Unreal.ClassOf<UTextBlock>()) as UTextBlock;
        Log.Write($"Mods button's text block: {(text != null ? UKismetSystemLibrary.GetObjectName(text) + " (" + GameUI.ClassPath(text) + ")" : "none")}");
        text?.SetText("Mods (test)");
        GameUI.Dump(modsButton, 3);
    }

    void OnModsClicked(UCommonButtonBase? button)
    {
        Log.Write("Mods (test) clicked: calling the system menu's AngelScript OnOpenSettingsClicked(button)");
        if (systemMenu != null && settingsButton != null) systemMenu.OnOpenSettingsClicked(settingsButton);
        Log.Write("Back from OnOpenSettingsClicked");
    }

    void FillSlots()
    {
        var tracker = GameUI.AddToSlot(this, GameSlots.QuestTracker, Unreal.ClassOf<TestSlot>(), 0);
        var system = GameUI.AddToSlot(this, GameSlots.SystemMenu, Unreal.ClassOf<TestSlot>(), 0);
        Log.Write($"F6: TestSlot in the quest tracker ({GameUI.SlotWidget(this, tracker) != null}) and the system menu slot ({GameUI.SlotWidget(this, system) != null})");
    }

    void OpenMenu()
    {
        var layer = World.LevelName(this).StartsWith("Menu") ? GameLayers.Menu : GameLayers.InGameMenu;
        var menu = GameUI.Push(this, layer, Unreal.ClassOf<TestMenu>());
        Log.Write($"F7: TestMenu on {layer}: {(menu != null ? "opened, activated " + menu.IsActivated() : "not opened")}");
    }
}

/// <summary>A label for the game's slots.</summary>
public class TestSlot : ModWidget
{
    protected override UWidget? Build(UWidgetTree tree) =>
        Ui.Panel(tree, Ui.Text(tree, "GameUiTest slot", 14), Ui.Color(0.8f, 0, 0.8f, 0.85f), 4);
}

/// <summary>A menu on the game's layers.</summary>
public class TestMenu : ModMenu
{
    protected override UWidget? Build(UWidgetTree tree)
    {
        var column = Ui.Column(tree);
        var close = Ui.Button(tree, "Close", 16);
        if (column == null || close == null) return null;
        column.AddChildToVerticalBox(Ui.Text(tree, "TestMenu, on the game's menu layer", 24));
        column.AddChildToVerticalBox(Ui.Text(tree, "The mouse should work. Esc and this button close it.", 14))?.SetPadding(Ui.Margin(8));
        column.AddChildToVerticalBox(close);
        close.OnClicked += CloseClicked;
        return Ui.Panel(tree, column, Ui.Color(0.03f, 0.03f, 0.05f, 0.95f), 24);
    }

    void CloseClicked()
    {
        Log.Write("TestMenu: Close clicked");
        Close();
    }
}

public class TestSave : USaveGame
{
    public bool LobbyTestDone;
}
