using UE.Engine;
using UE.SlateCore;
using UE.SpicewoodUI;
using UE.UMG;

namespace NeoRune
{
    /// <summary>
    /// A mod's own screen, opened on one of the game's layers like the game's screens:
    /// <code>
    /// public class MyMenu : ModMenu
    /// {
    ///     protected override UWidget? Build(UWidgetTree tree) => Ui.Panel(tree, Ui.Text(tree, "Hello", 24), Ui.Color(0, 0, 0, 0.8f), 24);
    /// }
    ///
    /// GameUI.Open(this, GameLayers.InGameMenu, Unreal.ClassOf&lt;MyMenu&gt;());
    /// </code>
    /// It's built on the game's own screen class: while it's open it takes the mouse and menu input, the game's back action
    /// (Esc, or B on a gamepad) closes it, and the game's fades and dialogs cover it. Build its content in <see cref="Build"/>.
    /// </summary>
    public abstract class ModMenu : USpicewoodActivatableWidget
    {
        public override void OnInitialized()
        {
            InputConfig = ESpicewoodWidgetInputMode.Menu;
            // The gamepad's B reaches the screen as CommonUI's back action (keys like Esc come to OnKeyDown).
            bIsBackHandler = true;
            // The screen takes the keyboard focus when it opens, so OnKeyDown gets Esc.
            bIsFocusable = true;
            // Opening and closing sound like the game's pause menu.
            ActivatedAudioTag = GameUI.Tag(Sounds.MenuOpenTag);
            DeactivatedAudioTag = GameUI.Tag(Sounds.MenuCloseTag);
            var tree = Ui.Tree(this);
            if (tree != null) tree.RootWidget = Build(tree);
        }

        protected override UWidget? BP_GetDesiredFocusTarget() => this;

        /// <summary>The back action (the gamepad's B): closes the screen.</summary>
        protected override bool BP_OnHandleBackAction()
        {
            Close();
            return true;
        }

        /// <summary>
        /// Esc and the gamepad's B close the screen. The game's own screens close through input actions a mod's class doesn't
        /// have, so the keys are handled here.
        /// </summary>
        public override FEventReply OnKeyDown(FGeometry MyGeometry, FKeyEvent InKeyEvent)
        {
            var key = UKismetInputLibrary.GetKey(InKeyEvent).KeyName;
            if (key != "Escape" && key != "Gamepad_FaceButton_Right") return UWidgetBlueprintLibrary.Unhandled();
            Close();
            return UWidgetBlueprintLibrary.Handled();
        }

        /// <summary>Makes the screen's content and returns its root. Create its widgets with <paramref name="tree"/> as their outer.</summary>
        protected abstract UWidget? Build(UWidgetTree tree);

        /// <summary>Makes a button play the game's click and hover sounds (<see cref="Sounds"/>). Returns the button.</summary>
        protected UButton? WithSounds(UButton? button)
        {
            if (button == null) return null;
            button.OnClicked += ClickSound;
            button.OnHovered += HoverSound;
            return button;
        }

        void ClickSound() => Sounds.Click(this);

        void HoverSound() => Sounds.Hover(this);

        /// <summary>Closes the screen.</summary>
        public void Close() => GameUI.Close(this);
    }

    /// <summary>
    /// A widget that builds its own content when it's created: for widgets the game creates from their class (a slot with
    /// <see cref="GameUI.AddToSlot"/>, a panel with <see cref="GameUI.AddToPanel"/>), or ones you create with <see cref="Ui.Create"/>.
    /// </summary>
    public abstract class ModWidget : UUserWidget
    {
        public override void OnInitialized()
        {
            var tree = Ui.Tree(this);
            if (tree != null) tree.RootWidget = Build(tree);
        }

        /// <summary>Makes the widget's content and returns its root. Runs once, when the widget is created.</summary>
        protected abstract UWidget? Build(UWidgetTree tree);

        /// <summary>Makes a button play the game's click and hover sounds (<see cref="Sounds"/>). Returns the button.</summary>
        protected UButton? WithSounds(UButton? button)
        {
            if (button == null) return null;
            button.OnClicked += ClickSound;
            button.OnHovered += HoverSound;
            return button;
        }

        void ClickSound() => Sounds.Click(this);

        void HoverSound() => Sounds.Hover(this);
    }
}
