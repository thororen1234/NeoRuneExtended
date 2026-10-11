using UE.Engine;

namespace NeoRune
{
    /// <summary>
    /// Your mod's text in the player's language. Put a Translations folder next to the .csproj with one file per language,
    /// named by its code (en.txt, de.txt, fr.txt, pt-BR.txt...), each with "key = value" lines:
    /// <code>
    /// # de.txt
    /// saved = Gespeichert!
    /// kills = {0} besiegt
    /// </code>
    /// Then <c>Translations.Get("saved")</c> gives the text in the game's current language: the exact one (pt-BR), else its
    /// main language (pt), else English (en.txt), else the key itself. A key missing from a language falls back to English.
    /// The files are built into the mod. Changing the game's language takes effect at once.
    /// </summary>
    public static class Translations
    {
        /// <summary>The text for <paramref name="key"/> in the game's language.</summary>
        public static string Get(string key)
        {
#if NEORUNE_TRANSLATIONS
            var culture = UKismetInternationalizationLibrary.GetCurrentCulture();
            if (culture != NeoRuneText.Table.Culture)
            {
                NeoRuneText.Table.Culture = culture;
                NeoRuneText.Table.Current.Clear();
                NeoRuneText.Table.Fallback.Clear();
                NeoRuneText.Table.Fill(NeoRuneText.Table.Pick(culture), NeoRuneText.Table.Current);
                NeoRuneText.Table.Fill(NeoRuneText.Table.FallbackLanguage, NeoRuneText.Table.Fallback);
            }
            if (NeoRuneText.Table.Current.TryGetValue(key, out var text)) return text;
            if (NeoRuneText.Table.Fallback.TryGetValue(key, out var fallback)) return fallback;
#endif
            return key;
        }

        /// <summary>The text for <paramref name="key"/> with {0} replaced by <paramref name="value"/>.</summary>
        public static string Get(string key, string value) => Get(key).Replace("{0}", value);

        /// <summary>The text for <paramref name="key"/> with {0} and {1} replaced.</summary>
        public static string Get(string key, string value0, string value1) => Get(key).Replace("{0}", value0).Replace("{1}", value1);

        /// <summary>The game's language right now, like "en-US" or "de-DE".</summary>
        public static string Culture => UKismetInternationalizationLibrary.GetCurrentCulture();
    }
}
