using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NeoRuneExtended.Discovery;

namespace NeoRuneExtended.Cli;

internal sealed record ModManifest(string Mod, string? NeoRune, GameBuild? Game, List<string> Uses, List<string>? Profile = null, string? Version = null, List<string>? Requires = null)
{
	public static List<string> CheckRequirements(IReadOnlyList<ModManifest> manifests, IReadOnlyCollection<string> installedFolders)
	{
		List<string> list = new List<string>();
		foreach (ModManifest manifest in manifests)
		{
			foreach (string item in manifest.Requires ?? new List<string>())
			{
				string[] array = item.Split(">=", 2, StringSplitOptions.TrimEntries);
				string name = array[0];
				ModManifest modManifest = manifests.FirstOrDefault((ModManifest o) => o.Mod.Equals(name, StringComparison.OrdinalIgnoreCase));
				if (!(modManifest != null) && !installedFolders.Any((string f) => f.Equals(name, StringComparison.OrdinalIgnoreCase)))
				{
					list.Add(manifest.Mod + " needs " + item + ", which isn't installed.");
				}
				else
				{
					if (array.Length < 2)
					{
						continue;
					}
					string text = modManifest?.Version;
					if (text == null)
					{
						continue;
					}
					Version version = ParseVersion(text);
					if ((object)version != null)
					{
						Version version2 = ParseVersion(array[1]);
						if ((object)version2 != null && version < version2)
						{
							list.Add($"{manifest.Mod} needs {name} {array[1]} or newer; {text} is installed.");
						}
					}
				}
			}
		}
		return list;
	}

	private static Version? ParseVersion(string text)
	{
		string text2 = new string(text.TakeWhile((char c) => char.IsDigit(c) || c == '.').ToArray()).Trim('.');
		if (text2.Length == 0)
		{
			return null;
		}
		if (!text2.Contains('.'))
		{
			text2 += ".0";
		}
		if (!System.Version.TryParse(text2, out Version result))
		{
			return null;
		}
		return result;
	}

	public static ModManifest? Read(string path)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(path));
			JsonElement rootElement = jsonDocument.RootElement;
			GameBuild game = ((rootElement.TryGetProperty("game", out var value) && value.ValueKind == JsonValueKind.Object) ? value.Deserialize<GameBuild>(ApiDump.Options) : null);
			List<string> uses = ((rootElement.TryGetProperty("uses", out var value2) && value2.ValueKind == JsonValueKind.Array) ? (from x in value2.EnumerateArray()
				select x.GetString()).OfType<string>().ToList() : new List<string>());
			List<string> profile = ((rootElement.TryGetProperty("profile", out var value3) && value3.ValueKind == JsonValueKind.Array) ? (from x in value3.EnumerateArray()
				select x.GetString() ?? "").ToList() : null);
			List<string> requires = ((rootElement.TryGetProperty("requires", out var value4) && value4.ValueKind == JsonValueKind.Array) ? (from x in value4.EnumerateArray()
				select x.GetString() ?? "" into x
				where x.Length > 0
				select x).ToList() : null);
			JsonElement value5;
			JsonElement value6;
			return new ModManifest(rootElement.GetProperty("mod").GetString() ?? "", rootElement.TryGetProperty("neoRune", out value5) ? value5.GetString() : null, game, uses, profile, rootElement.TryGetProperty("version", out value6) ? value6.GetString() : null, requires);
		}
		catch (Exception ex) when (((ex is JsonException || ex is IOException || ex is KeyNotFoundException || ex is InvalidOperationException) ? 1 : 0) != 0)
		{
			return null;
		}
	}
}
