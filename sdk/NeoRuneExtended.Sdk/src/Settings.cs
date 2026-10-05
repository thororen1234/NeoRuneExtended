using System.Collections.Generic;
using UE.Engine;

namespace NeoRune
{
    /// <summary>Storage for <see cref="Settings"/>: SaveGames/NeoRune_&lt;Mod&gt;_Settings.sav.</summary>
    public class NeoRuneSettingsData : USaveGame
    {
        public Dictionary<string, string> Values = new();
    }

    /// <summary>
    /// A mod's settings. Mods can't read text or config files (the game has no file-reading functions for them), so
    /// settings live in a save game, changed from your own in-game options with <see cref="Set"/>, and players can
    /// override any of them with a launch option: -&lt;Mod&gt;.&lt;key&gt;=value (e.g. -BetterArmor.HideHelmet=true in Steam's launch
    /// options). Each Get reads the save, so read your settings once, e.g. in ReceiveBeginPlay.
    /// </summary>
    public static class Settings
    {
        /// <summary>The launch option -&lt;Mod&gt;.&lt;key&gt;=value if given, else the saved value, else fallback.</summary>
        public static string Get(string key, string fallback)
        {
            if (UKismetSystemLibrary.ParseParamValue(UKismetSystemLibrary.GetCommandLine(), $"{Unreal.ModName}.{key}=", out var fromCommandLine))
                return fromCommandLine;
            var data = Load();
            if (data != null && data.Values.TryGetValue(key, out var saved)) return saved;
            return fallback;
        }

        public static int GetInt(string key, int fallback)
        {
            var text = Get(key, "");
            return text.Length > 0 ? UKismetStringLibrary.Conv_StringToInt(text) : fallback;
        }

        /// <summary>true / 1 / yes / on (any case) are true.</summary>
        public static bool GetBool(string key, bool fallback)
        {
            var text = Get(key, "").ToLower();
            if (text.Length == 0) return fallback;
            return text == "true" || text == "1" || text == "yes" || text == "on";
        }

        /// <summary>Saves a value (launch options still win over it).</summary>
        public static void Set(string key, string value)
        {
            var data = Load();
            if (data == null) data = UGameplayStatics.CreateSaveGameObject(Unreal.ClassOf<NeoRuneSettingsData>()) as NeoRuneSettingsData;
            if (data == null) return;
            data.Values[key] = value;
            UGameplayStatics.SaveGameToSlot(data, Slot(), 0);
        }

        public static void Remove(string key)
        {
            var data = Load();
            if (data == null || !data.Values.Remove(key)) return;
            UGameplayStatics.SaveGameToSlot(data, Slot(), 0);
        }

        static NeoRuneSettingsData? Load() => UGameplayStatics.LoadGameFromSlot(Slot(), 0) as NeoRuneSettingsData;

        static string Slot() => $"NeoRune_{Unreal.ModName}_Settings";
    }
}
