using System.Collections.Generic;
using UE.AkAudio;
using UE.CommonUI;
using UE.CoreUObject;
using UE.Engine;
using UE.MSAudioRuntime;
using UE.SWUIConfig;
using UE.UMG;

namespace NeoRune
{
    /// <summary>
    /// The game's own UI sounds. The game names them with tags (SW.UI.Audio...), the same ones its screens use:
    /// <code>
    /// Sounds.Click(this);   // selecting something (the hero selection's select sound)
    /// Sounds.Hover(this);   // the pointer moving onto it
    /// Sounds.Play(this, "SW.UI.Audio.General.Forbidden.Action");
    /// </code>
    /// <see cref="LogTags"/> logs the game's UI sound tags. The game has no single "button click" sound (its menus each have
    /// their own), so Click, Hover and Confirm are the hero selection's, which suit most menus.
    /// </summary>
    public static class Sounds
    {
        public const string ClickTag = "SW.UI.Audio.SkinSelection.SkinSelect";
        public const string HoverTag = "SW.UI.Audio.SkinSelection.SkinHover";
        public const string ConfirmTag = "SW.UI.Audio.SkinSelection.SkinConfirm";
        public const string ForbiddenTag = "SW.UI.Audio.General.Forbidden.Action";
        public const string ToastTag = "SW.UI.Audio.Lobby.Toast.Appear";
        /// <summary>The pause menu opening and closing (ModMenu uses them).</summary>
        public const string MenuOpenTag = "SW.UI.WidgetName.SystemMenu.Audio.Activate";
        public const string MenuCloseTag = "SW.UI.WidgetName.SystemMenu.Audio.Deactivate";

        /// <summary>Plays one of the game's UI sounds by its tag (SW.UI.Audio...).</summary>
        public static void Play(UObject context, string audioTag) =>
            USWUIConfigLibrary.PostGlobalEventFromTag(GameUI.Tag(audioTag), EAudioGlobalEmitterType.Default);

        public static void Click(UObject context) => Play(context, ClickTag);

        public static void Hover(UObject context) => Play(context, HoverTag);

        public static void Confirm(UObject context) => Play(context, ConfirmTag);

        /// <summary>The game's "can't do that" sound.</summary>
        public static void Forbidden(UObject context) => Play(context, ForbiddenTag);

        /// <summary>Logs the game's UI sound tags (for <see cref="Play"/>), by the list they're in.</summary>
        public static void LogTags(UObject context)
        {
            var config = USWUIConfigLibrary.GetAudio();
            if (config == null) { Log.Write("Sounds: the game's UI audio settings aren't available"); return; }
            LogList("Common", config.CommonEvents);
            LogList("Widget", config.WidgetEvents);
            LogList("Menu", config.MenuEvents);
            LogList("MainMenu", config.MainMenuEvents);
            LogList("SystemMenu", config.SystemMenuEvents);
            LogList("Notification", config.NotificationEvents);
            LogList("InGameNavigation", config.InGameNavigationEvents);
            LogList("HUD", config.HUDEvents);
            LogList("Inventory", config.InventoryEvents);
            LogList("SettingsMenu", config.SettingsMenuEvents);
        }

        static void LogList(string name, List<FUIAudioEventDefinition> events)
        {
            var tags = new List<string>();
            foreach (var e in events) tags.Add(e.AudioTag.TagName.ToString());
            Log.Write($"Sounds {name}: {string.Join(", ", tags)}");
        }

        /// <summary>Plays a sound asset of the game: a sound (USoundBase) or a Wwise event (UAkAudioEvent).</summary>
        public static void PlayAsset(UObject context, UObject? sound)
        {
            var player = SoundPlayer.For(context);
            player?.Play(sound);
        }
    }

    /// <summary>Plays sound assets for <see cref="Sounds.PlayAsset"/> (Wwise needs a callback method). Lives with the level.</summary>
    public class SoundPlayer : AActor
    {
        public static SoundPlayer? For(UObject context)
        {
            UGameplayStatics.GetAllActorsOfClass(context, Unreal.ClassOf<SoundPlayer>(), out var found);
            if (found.Count > 0 && found[0] is SoundPlayer existing) return existing;
            return World.Spawn(context, Unreal.ClassOf<SoundPlayer>(), new FVector()) as SoundPlayer;
        }

        public void Play(UObject? sound)
        {
            if (sound is USoundBase plain) UGameplayStatics.PlaySound2D(this, plain, 1f, 1f, 0f, null, null, true);
            else if (sound is UAkAudioEvent wwise) wwise.PostOnActor(this, NoCallback, 0, false);
        }

        /// <summary>Wwise wants a callback; with an empty mask it's never called.</summary>
        void NoCallback(EAkCallbackType type, UAkCallbackInfo? info) { }
    }
}
