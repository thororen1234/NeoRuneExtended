using UE.CoreUObject;
using UE.SpicewoodUI;
using UE.UMG;

namespace NeoRune
{
    /// <summary>
    /// A mod's own screen, opened on one of the game's layers like the game's screens:
    /// <code>
    /// public class MyMenu : ModMenu
    /// {
    ///     protected override UWidget? Build(UWidgetTree tree)
    ///     {
    ///         var column = Ui.Column(tree);
    ///         column?.AddChildToVerticalBox(Ui.Text(tree, "Hello", 24));
    ///         return Ui.Panel(tree, column, Ui.Color(0, 0, 0, 0.8f), 24);
    ///     }
    /// }
    ///
    /// GameUI.Push(this, GameLayers.InGameMenu, Unreal.ClassOf&lt;MyMenu&gt;());
    /// </code>
    /// It's built on the game's own screen class: it takes the mouse and menu input while it's open, the game's back action
    /// (Esc / B) closes it, and it's covered by the game's fades and dialogs. Build its content in <see cref="Build"/> and
    /// don't override OnInitialized, which calls it.
    /// </summary>
    public abstract class ModMenu : USpicewoodActivatableWidget
    {
        public override void OnInitialized()
        {
            InputConfig = ESpicewoodWidgetInputMode.Menu;
            bIsBackHandler = true;
            var tree = Ui.Tree(this);
            if (tree == null) return;
            tree.RootWidget = Build(tree);
        }

        /// <summary>
        /// Makes the screen's content and returns its root. Create the widgets with <paramref name="tree"/> as their outer,
        /// e.g. <c>Ui.Text(tree, "Hello", 24)</c>. Runs once, when the layer creates the screen.
        /// </summary>
        protected abstract UWidget? Build(UWidgetTree tree);

        /// <summary>Closes the screen.</summary>
        public void Close() => GameUI.Close(this);
    }

    /// <summary>
    /// A widget that builds its own content when it's created: for widgets something else creates from their class, like a
    /// game slot (<see cref="GameUI.AddToSlot"/>), and for widgets you put into the game's panels. Override <see cref="Build"/>.
    /// Create one yourself with <see cref="Ui.Create"/>.
    /// </summary>
    public abstract class ModWidget : UUserWidget
    {
        public override void OnInitialized()
        {
            var tree = Ui.Tree(this);
            if (tree == null) return;
            tree.RootWidget = Build(tree);
        }

        /// <summary>Makes the widget's content and returns its root. Runs once, when the widget is created.</summary>
        protected abstract UWidget? Build(UWidgetTree tree);
    }
}
