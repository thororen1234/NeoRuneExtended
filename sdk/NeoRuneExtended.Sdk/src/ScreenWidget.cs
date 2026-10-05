using UE.CoreUObject;
using UE.UMG;

namespace NeoRune
{
    /// <summary>
    /// Base class for a mod's own widget on screen. <see cref="ShowAt(FVector2D, FVector2D, int)"/> puts it either under
    /// the game's UI (on the player's screen, so the game's fades, loading screens and dialogs cover it like the game's own
    /// widgets) or above all of it (on the viewport: clickable over the game's menus, but not covered by fades). A shown widget puts itself
    /// back if the game rebuilds its UI and takes it off the screen; <see cref="Hide"/> takes it off for good.
    /// </summary>
    public abstract class ScreenWidget : UUserWidget
    {
        /// <summary>Under the game's UI: covered by its fades and dialogs, but the game's menus take its clicks.</summary>
        public const int UnderGameUI = -10;
        /// <summary>Above all of the game's UI (on the viewport): clickable over its menus, but fades don't cover it.</summary>
        public const int AboveGameUI = 10000;

        FVector2D shownAt;
        FVector2D shownAnchor;
        int shownZOrder;

        /// <summary>Shows the widget under the game's UI with its top left corner at a position (pixels at 1080p, scaled with the UI).</summary>
        public void ShowAt(FVector2D position) => ShowAt(position, new FVector2D(), UnderGameUI);

        /// <summary>
        /// Shows the widget at an offset from an anchor point of the screen: (0, 0) top left, (0.5, 0) top centre, (1, 1) bottom
        /// right. The widget is aligned the same way, so with (1, 0) its right edge sits on the screen's right edge; move it
        /// in with a negative offset, e.g. (-16, 16).
        /// zOrder: <see cref="UnderGameUI"/> for things to look at, <see cref="AboveGameUI"/> for anything with buttons. If the
        /// player doesn't exist yet (right at the start of a level), it's shown as soon as it does.
        /// </summary>
        public void ShowAt(FVector2D offset, FVector2D anchor, int zOrder)
        {
            shownAt = offset;
            shownAnchor = anchor;
            shownZOrder = zOrder;
            Attach();
            // The game can rebuild its UI and take the widget off the screen with it: put it back when that happens.
            // (Also covers the player not existing yet right at the start of a level.)
            Timer.Start(this, nameof(KeepShown), 0.5f, loop: true);
        }

        /// <summary>Takes the widget off the screen for good (RemoveFromParent alone would be undone by ShowAt's check).</summary>
        public void Hide()
        {
            Timer.Stop(this, nameof(KeepShown));
            RemoveFromParent();
        }

        /// <summary>
        /// Under the game's UI: the player's screen, where the game's UI lives (it needs the player). Above it: the viewport.
        /// Once the main menu is up, the game's UI layout takes every click on the player's screen whatever the z-order,
        /// while widgets on the viewport still get them.
        /// </summary>
        void Attach()
        {
            bool above = shownZOrder > 0;
            var player = World.PlayerController(this);
            if (player == null && !above) return;
            if (player != null) SetOwningPlayer(player);
            // SetPositionInViewport resets the anchors: it comes first.
            SetPositionInViewport(shownAt, true);
            SetAnchorsInViewport(new UE.Slate.FAnchors { Minimum = shownAnchor, Maximum = shownAnchor });
            SetAlignmentInViewport(shownAnchor);
            if (above) AddToViewport(shownZOrder);
            else AddToPlayerScreen(shownZOrder);
        }

        void KeepShown()
        {
            if (!IsInViewport()) Attach();
        }
    }
}
