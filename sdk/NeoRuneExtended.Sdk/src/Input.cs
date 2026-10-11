using UE.AngelscriptCode;
using UE.CoreUObject;
using UE.Engine;
using UE.InputCore;

namespace NeoRune
{
    /// <summary>
    /// Keys without checking them every frame: the game's input system calls your method when the key is pressed.
    /// <code>
    /// menuKey = Input.OnKey(this, "F7");
    /// if (menuKey != null) menuKey.Pressed += OpenMenu;
    /// </code>
    /// With a Keybind setting, start a new listener when the player picks another key:
    /// <code>
    /// public void OnKeybindChanged(string id, FKey key, FKey secondaryKey)
    /// {
    ///     menuKey?.Stop();
    ///     menuKey = Input.OnKey(this, key);
    ///     if (menuKey != null) menuKey.Pressed += OpenMenu;
    /// }
    /// </code>
    /// Key names are Unreal's: "F7", "M", "LeftShift", "Gamepad_FaceButton_Top"... Keys reach mods while the player is
    /// in the game (not while a menu has the keyboard, and not while the game is paused). A listener ends with the level.
    /// </summary>
    public static class Input
    {
        public static KeyListener? OnKey(UObject context, string key) => OnKey(context, new FKey { KeyName = key });

        public static KeyListener? OnKey(UObject context, FKey key)
        {
            var player = World.PlayerController(context);
            if (player == null) return null;
            var listener = World.Spawn(context, Unreal.ClassOf<KeyListener>(), new FVector()) as KeyListener;
            if (listener == null) return null;
            listener.Listen(player, key);
            return listener;
        }
    }

    /// <summary>Made by <see cref="Input.OnKey(UObject, string)"/>: raises Pressed and Released for its key.</summary>
    public class KeyListener : AActor
    {
        public event TimerEvent Pressed;
        public event TimerEvent Released;

        public void Listen(APlayerController player, FKey key)
        {
            // The actor's input component joins the player's input stack; the key calls back only when it's pressed.
            EnableInput(player);
            UInputComponentScriptMixinLibrary.BindKey(InputComponent, key, EInputEvent.IE_Pressed, OnPressed, false);
            UInputComponentScriptMixinLibrary.BindKey(InputComponent, key, EInputEvent.IE_Released, OnReleased, false);
        }

        /// <summary>Stops listening (and removes the listener).</summary>
        public void Stop() => K2_DestroyActor();

        void OnPressed(FKey key) => Pressed();

        void OnReleased(FKey key) => Released();
    }
}
