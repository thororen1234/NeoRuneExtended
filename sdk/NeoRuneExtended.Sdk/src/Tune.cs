using System.Collections.Generic;
using System.Linq;
using UE.CoreUObject;
using UE.Engine;

namespace NeoRune
{
    /// <summary>The handler of <see cref="Tuner.Changed"/>: a method without parameters.</summary>
    [UDelegate("/Script/UMG.OnButtonClickedEvent__DelegateSignature")]
    public delegate void TuneEvent();

    /// <summary>
    /// Live tuning: change a running mod's numbers, text and switches from the command line, without restarting the game.
    /// <code>
    /// tuner = Tuner.Start(this);
    /// tuner.Changed += Apply;
    /// Apply();
    /// ...
    /// void Apply() => banner?.SetRenderOpacity((float)tuner.Number("opacity", 0.8));
    /// </code>
    /// Then, while the game runs: <c>neorunex tune MyMod</c> and type <c>opacity=0.5</c>. Every value has the default you
    /// pass in code, which is what the mod uses when nobody is tuning (and what a Release build always uses).
    /// <para>
    /// Only Debug builds (the default of <c>dotnet build</c>) listen, and only while "neorunex tune" runs: they look for its
    /// flag file once a second (one file check) and ask it for changes over a local connection. Release builds
    /// (<c>dotnet build -c Release</c>, or <c>&lt;NeoRuneLiveTuning&gt;false&lt;/NeoRuneLiveTuning&gt;</c>) don't listen
    /// at all. Tuned values aren't saved: put the ones you like in the code.
    /// </para>
    /// </summary>
    public class Tuner : UObject
    {
        /// <summary>Port of "neorunex tune" on this computer.</summary>
        public const int Port = 47321;

        /// <summary>Runs after a value changed (the values are already updated).</summary>
        public event TuneEvent Changed;

        public Dictionary<string, string> Values = new();
        /// <summary>Every key the code asked for, with its default: sent to "neorunex tune" so it can list them.</summary>
        public Dictionary<string, string> Defaults = new();
        string version;
        /// <summary>A GET is waiting for its answer (request number askingFor).</summary>
        bool asking;
        int askingFor;
        bool defaultsSent;
        int keysSent;
        Http? http;

        public static Tuner? Start(UObject owner)
        {
            var tuner = UGameplayStatics.SpawnObject(Unreal.ClassOf<Tuner>(), owner) as Tuner;
#if NEORUNE_TUNING
            if (tuner != null) UKismetSystemLibrary.K2_SetTimer(tuner, "Check", 1f, true, false, 0f, 0f);
#endif
            return tuner;
        }

        public double Number(string key, double fallback)
        {
            Remember(key, $"{fallback}");
            return Values.TryGetValue(key, out var text) && UKismetStringLibrary.IsNumeric(text) ? double.Parse(text) : fallback;
        }

        public int Whole(string key, int fallback) => (int)System.Math.Round(Number(key, fallback));

        public string Text(string key, string fallback)
        {
            Remember(key, fallback);
            return Values.TryGetValue(key, out var text) ? text : fallback;
        }

        public bool Switch(string key, bool fallback)
        {
            Remember(key, fallback ? "true" : "false");
            if (!Values.TryGetValue(key, out var text)) return fallback;
            return text.Equals("true", System.StringComparison.OrdinalIgnoreCase) || text == "1" || text.Equals("on", System.StringComparison.OrdinalIgnoreCase);
        }

        void Remember(string key, string fallback)
        {
            if (!Defaults.ContainsKey(key)) Defaults[key] = fallback;
        }

        /// <summary>Once a second (Debug builds): while "neorunex tune" runs (its flag file exists), asks it for changes.</summary>
        void Check()
        {
            if (asking) return;
            var flag = UKismetSystemLibrary.GetProjectSavedDirectory() + "NeoRune/tune-" + Unreal.ModName + ".on";
            if (!UBlueprintPathsLibrary.FileExists(flag)) { defaultsSent = false; return; }
            if (http == null)
            {
                http = Http.Create(this);
                if (http == null) return;
                http.Responded += OnResponse;
            }
            var url = $"http://127.0.0.1:{Port}/{Unreal.ModName}";
            // The keys (and their defaults) once per session and when the code asked for new ones, so "neorunex tune" can list them.
            if (!defaultsSent || keysSent != Defaults.Count)
            {
                var body = "";
                foreach (var pair in Defaults) body += pair.Key + "=" + pair.Value + "\n";
                http.Post(url + "/defaults", body, "text/plain");
                defaultsSent = true;
                keysSent = Defaults.Count;
            }
            askingFor = http.Get(url);
            asking = askingFor >= 0;
        }

        /// <summary>The values: a version line, then key=value lines.</summary>
        void OnResponse(HttpResponse response)
        {
            if (!asking || response.Request != askingFor) return;   // the answer to a POST of the defaults
            asking = false;
            if (response.Status != 200 || !response.Body.StartsWith("v")) return;
            var lines = response.Body.Split('\n');
            if (lines.Length == 0 || lines[0] == version) return;
            version = lines[0];
            var values = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                var at = line.IndexOf("=");
                if (at > 0) values[line.Substring(0, at).Trim()] = line.Substring(at + 1).Trim();
            }
            Values = values;
            Log.Write("tuned: " + string.Join(", ", lines.Skip(1).Where(l => l.Length > 0).ToList()));
            Changed?.Invoke();
        }
    }
}
