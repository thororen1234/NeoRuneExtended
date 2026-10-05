using UE.CoreUObject;
using UE.Engine;
using UE.InputCore;

namespace NeoRune
{
    /// <summary>
    /// The settings page events of Blueprint Loader 2.0's Mods tab. Implement it on ModActor (and on [ModSetting.Widget]
    /// widgets) and write the methods you need; the others do nothing. Needs Blueprint Loader 2.0 or newer.
    /// </summary>
    [UClass("/Game/Mods/BlueprintLoader/BPI_ModSettings.BPI_ModSettings_C")]
    public interface IModSettings
    {
        /// <summary>
        /// A setting changed: "true"/"false" for a toggle, the number for a slider, the option index for a select, the text
        /// for a text input, a hex colour for a colour. Also called when the mod loads, for every setting that isn't on its
        /// default: give your fields the same defaults as the settings.
        /// </summary>
        void OnSettingChanged(string id, string value) { }
        /// <summary>A keybind changed (also called when the mod loads, like OnSettingChanged).</summary>
        void OnKeybindChanged(string id, FKey key, FKey secondaryKey) { }
        /// <summary>An event button was clicked.</summary>
        void OnButtonPressed(string id) { }
        /// <summary>The player reset the mod's settings: set your fields back to their defaults.</summary>
        void OnSettingsReset() { }
        /// <summary>This widget was added to the settings page ([ModSetting.Widget] widgets only).</summary>
        void OnWidgetAdded(string id) { }
    }

    /// <summary>
    /// For a [ModSetting.Widget] widget that saves its own values: shows the page's Reset button even when the page has
    /// no other settings.
    /// </summary>
    [UClass("/Game/Mods/BlueprintLoader/BPI_ModResettable.BPI_ModResettable_C")]
    public interface IModResettable { }

    /// <summary>
    /// Reading the values sent to <see cref="IModSettings.OnSettingChanged"/>, and saving values in Blueprint Loader 2.0's
    /// settings. Saving under a setting's id changes that setting (e.g. a key that toggles an option). Saving and reading
    /// need Blueprint Loader 2.0 or newer; <see cref="Settings"/> works without it.
    /// </summary>
    public static class ModSettings
    {
        /// <summary>A toggle's value: "true" or "false".</summary>
        public static bool ToBool(string value) => value == "true";
        /// <summary>A slider's value, e.g. "0.75".</summary>
        public static double ToNumber(string value) => UKismetStringLibrary.Conv_StringToDouble(value);
        /// <summary>A select's option index, e.g. "2".</summary>
        public static int ToInt(string value) => UKismetStringLibrary.Conv_StringToInt(value);

        /// <summary>A colour's value ("#FF8800" or "#FF8800CC") as the linear colour widgets use; white if it isn't one.</summary>
        public static FLinearColor ToColour(string value)
        {
            var digits = UKismetStringLibrary.ToUpper(UKismetStringLibrary.Replace(value, "#", "", ESearchCase.IgnoreCase));
            int length = UKismetStringLibrary.Len(digits);
            if (length != 6 && length != 8) return new FLinearColor { R = 1, G = 1, B = 1, A = 1 };
            var colour = new FColor
            {
                R = UKismetMathLibrary.Conv_IntToByte(HexByte(digits, 0)),
                G = UKismetMathLibrary.Conv_IntToByte(HexByte(digits, 2)),
                B = UKismetMathLibrary.Conv_IntToByte(HexByte(digits, 4)),
                A = UKismetMathLibrary.Conv_IntToByte(length == 8 ? HexByte(digits, 6) : 255),
            };
            // FColor is sRGB: the conversion gives the linear colour, as the colour picker does.
            return UKismetMathLibrary.Conv_ColorToLinearColor(colour);
        }

        static int HexByte(string digits, int at) => HexDigit(digits, at) * 16 + HexDigit(digits, at + 1);
        static int HexDigit(string digits, int at) =>
            UKismetStringLibrary.FindSubstring("0123456789ABCDEF", UKismetStringLibrary.GetSubstring(digits, at, 1), false, false, 0);

        /// <summary>Saves a text value. Pass the mod's actor (or widget) as <paramref name="mod"/>.</summary>
        public static void Set(UObject mod, string id, string value) => BFL_ModSettings.SetModSetting(mod, id, value, mod);
        public static void SetBool(UObject mod, string id, bool value) => BFL_ModSettings.SetModSettingBool(mod, id, value, mod);
        public static void SetNumber(UObject mod, string id, double value) => BFL_ModSettings.SetModSettingNumber(mod, id, value, mod);
        public static void SetInt(UObject mod, string id, int value) => BFL_ModSettings.SetModSettingInt(mod, id, value, mod);
        public static void SetKeybind(UObject mod, string id, FKey key, FKey secondaryKey) => BFL_ModSettings.SetModKeybind(mod, id, key, secondaryKey, mod);

        /// <summary>A saved text value, or <paramref name="fallback"/> when nothing is saved under the id.</summary>
        public static string Get(UObject mod, string id, string fallback)
        {
            BFL_ModSettings.GetModSetting(mod, id, fallback, mod, out var value, out _);
            return value;
        }

        public static bool GetBool(UObject mod, string id, bool fallback)
        {
            BFL_ModSettings.GetModSettingBool(mod, id, fallback, mod, out var value, out _);
            return value;
        }

        public static double GetNumber(UObject mod, string id, double fallback)
        {
            BFL_ModSettings.GetModSettingNumber(mod, id, fallback, mod, out var value, out _);
            return value;
        }

        public static int GetInt(UObject mod, string id, int fallback)
        {
            BFL_ModSettings.GetModSettingInt(mod, id, fallback, mod, out var value, out _);
            return value;
        }
    }

    /// <summary>
    /// Blueprint Loader 2.0's BFL_ModSettings. The parameter order is what the game calls: inputs, then the hidden world
    /// context, then the outputs (checked against Blueprint Loader's own cooked calls).
    /// </summary>
    [UClass("/Game/Mods/BlueprintLoader/BFL_ModSettings.BFL_ModSettings_C")]
    public abstract class BFL_ModSettings : UBlueprintFunctionLibrary
    {
        const uint Flags = UFunctionFlags.Static | UFunctionFlags.Public | UFunctionFlags.BlueprintCallable | UFunctionFlags.BlueprintEvent;
        const uint GetFlags = Flags | UFunctionFlags.HasOutParms;
        const string Mod = "Mod:object(/Script/CoreUObject.Object):80;Id:string:80";
        const string WorldContext = "__WorldContext:object(/Script/CoreUObject.Object):80";

        [UFunction("SetModSetting", Flags, Mod + ";Value:string:80;" + WorldContext)]
        public static void SetModSetting(UObject mod, string id, string value, UObject worldContext) => throw null!;
        [UFunction("SetModSettingBool", Flags, Mod + ";Value:bool:80;" + WorldContext)]
        public static void SetModSettingBool(UObject mod, string id, bool value, UObject worldContext) => throw null!;
        [UFunction("SetModSettingNumber", Flags, Mod + ";Value:double:80;" + WorldContext)]
        public static void SetModSettingNumber(UObject mod, string id, double value, UObject worldContext) => throw null!;
        [UFunction("SetModSettingInt", Flags, Mod + ";Value:int:80;" + WorldContext)]
        public static void SetModSettingInt(UObject mod, string id, int value, UObject worldContext) => throw null!;
        [UFunction("SetModKeybind", Flags, Mod + ";Key:struct(/Script/InputCore.Key,24):80;SecondaryKey:struct(/Script/InputCore.Key,24):80;" + WorldContext)]
        public static void SetModKeybind(UObject mod, string id, FKey key, FKey secondaryKey, UObject worldContext) => throw null!;

        [UFunction("GetModSetting", GetFlags, Mod + ";Default:string:80;" + WorldContext + ";Value:string:180;Found:bool:180")]
        public static void GetModSetting(UObject mod, string id, string fallback, UObject worldContext, out string value, out bool found) => throw null!;
        [UFunction("GetModSettingBool", GetFlags, Mod + ";Default:bool:80;" + WorldContext + ";Value:bool:180;Found:bool:180")]
        public static void GetModSettingBool(UObject mod, string id, bool fallback, UObject worldContext, out bool value, out bool found) => throw null!;
        [UFunction("GetModSettingNumber", GetFlags, Mod + ";Default:double:80;" + WorldContext + ";Value:double:180;Found:bool:180")]
        public static void GetModSettingNumber(UObject mod, string id, double fallback, UObject worldContext, out double value, out bool found) => throw null!;
        [UFunction("GetModSettingInt", GetFlags, Mod + ";Default:int:80;" + WorldContext + ";Value:int:180;Found:bool:180")]
        public static void GetModSettingInt(UObject mod, string id, int fallback, UObject worldContext, out int value, out bool found) => throw null!;
    }

    /// <summary>UFunction flags for hand-written bindings.</summary>
    static class UFunctionFlags
    {
        public const uint Static = 0x2000, Public = 0x20000, HasOutParms = 0x400000, BlueprintCallable = 0x4000000, BlueprintEvent = 0x8000000;
    }
}
