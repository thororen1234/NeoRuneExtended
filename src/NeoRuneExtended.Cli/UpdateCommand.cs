using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using NeoRuneExtended.Bindings;
using NeoRuneExtended.Discovery;
using NeoRuneExtended.Packaging;

namespace NeoRuneExtended.Cli;

internal static class UpdateCommand
{
	public static string CacheDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NeoRune", "api");

	private static IEnumerable<string> DumpFolders()
	{
		yield return CacheDirectory;
		yield return Path.Combine(AppContext.BaseDirectory, "api");
	}

	private static string? FindDump(GameBuild build)
	{
		List<string> list = DumpFolders().Where(Directory.Exists).SelectMany((string d) => Directory.EnumerateFiles(d, "*.json.gz")).ToList();
		string text = list.FirstOrDefault((string f) => (build.BuildId.Length > 0 && Stem(f) == build.BuildId) || (build.ExeStamp.Length > 0 && Stem(f) == build.ExeStamp));
		if (text != null)
		{
			return text;
		}
		foreach (string item in list)
		{
			try
			{
				if (GameInstall.SameBuild(BindingsProject.LoadDump(item).Game, build) == true)
				{
					return item;
				}
			}
			catch (Exception ex) when (((ex is IOException || ex is JsonException || ex is InvalidDataException) ? 1 : 0) != 0)
			{
			}
		}
		return null;
		static string Stem(string f)
		{
			string fileName = Path.GetFileName(f);
			int length = ".json.gz".Length;
			return fileName.Substring(0, fileName.Length - length);
		}
	}

	public static int Run(Args a)
	{
		string game = ModPackager.ResolveGame(a.Get("game"));
		GameBuild installed = GameInstall.Identify(game);
		Console.WriteLine("Game: " + GameInstall.Describe(installed));
		string path = GameInstall.ModsDirectory(game);
		List<ModManifest> list = new List<ModManifest>();
		List<string> list2 = new List<string>();
		if (Directory.Exists(path))
		{
			foreach (string item in Directory.EnumerateDirectories(path).OrderBy<string, string>((string d) => d, StringComparer.OrdinalIgnoreCase))
			{
				string fileName = Path.GetFileName(item);
				if (fileName.StartsWith("BlueprintLoader", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				string text = Directory.EnumerateFiles(item, "*.neorune.json").FirstOrDefault();
				if (text != null)
				{
					ModManifest modManifest = ModManifest.Read(text);
					if ((object)modManifest != null)
					{
						list.Add(modManifest);
						continue;
					}
				}
				list2.Add(fileName);
			}
		}
		if (list.Count == 0)
		{
			Console.WriteLine((list2.Count == 0) ? "No mods installed." : ("No NeoRune 1.0 mods installed (mods built before NeoRune 1.0 have no manifest to check): " + string.Join(", ", list2) + "."));
			return 0;
		}
		string text2 = a.Get("api") ?? FindDump(installed);
		if (list.Where((ModManifest modManifest2) => modManifest2.Game == null || GameInstall.SameBuild(modManifest2.Game, installed) != true).ToList().Count > 0 && text2 == null)
		{
			if (!GameInstall.IsRunning())
			{
				Console.WriteLine("NeoRune doesn't know this game build yet. Start the game, wait for the main menu, and run \"neorunex update\" again: it reads the game's API (read-only).");
				Console.WriteLine("If the game crashes at startup because of an outdated mod, move that mod's folder out of ~mods first.");
				return 1;
			}
			Console.WriteLine("Reading the running game's API (read-only)...");
			text2 = SaveDump(Discover());
		}
		ApiDump newDump = ((text2 != null) ? BindingsProject.LoadDump(text2) : null);
		Dictionary<string, List<ApiChange>> dictionary = new Dictionary<string, List<ApiChange>>();
		int num = 0;
		Console.WriteLine();
		foreach (ModManifest m in list)
		{
			string text3 = ((m.NeoRune != null) ? (m.Mod + " (NeoRune " + m.NeoRune + ")") : m.Mod);
			if (m.Game != null && GameInstall.SameBuild(m.Game, installed) == true)
			{
				Console.WriteLine("  [ok]      " + text3 + ": made for this game build.");
				continue;
			}
			if (m.Game != null)
			{
				string text4 = FindDump(m.Game);
				if (text4 != null)
				{
					string key = text4;
					if (!dictionary.TryGetValue(key, out var value))
					{
						value = (dictionary[key] = ApiDiff.Changes(BindingsProject.LoadDump(text4), newDump));
					}
					List<ApiChange> list4 = value.Where((ApiChange c) => c.Affects(m.Uses)).ToList();
					if (list4.Count == 0)
					{
						Console.WriteLine($"  [ok]      {text3}: made for {GameInstall.Describe(m.Game)}, but none of the {m.Uses.Count} game classes, functions and properties it uses changed. It should keep working.");
						continue;
					}
					num++;
					Console.WriteLine($"  [BREAKS]  {text3}: uses {list4.Count} thing(s) this game update removed or changed:");
					foreach (ApiChange item2 in list4.Take(12))
					{
						Console.WriteLine("              " + item2.Text);
					}
					if (list4.Count > 12)
					{
						Console.WriteLine($"              ... and {list4.Count - 12} more");
					}
					Console.WriteLine("            Its author needs to rebuild it with an SDK made for this game build (and fix what changed); until then, remove it from ~mods.");
					continue;
				}
			}
			Console.WriteLine($"  [unknown] {text3}: made for {((m.Game != null) ? GameInstall.Describe(m.Game) : "an unknown build")}, whose API this NeoRuneExtended doesn't have. Update it (dotnet tool update -g NeoRuneExtended.Tool).");
		}
		if (list2.Count > 0)
		{
			Console.WriteLine("  Not checked (not NeoRune mods, or built before NeoRune 1.0): " + string.Join(", ", list2));
		}
		foreach (string item3 in ModManifest.CheckRequirements(list, list.Select((ModManifest modManifest2) => modManifest2.Mod).Concat(list2).ToList()))
		{
			num++;
			Console.WriteLine("  [missing] " + item3);
		}
		Console.WriteLine();
		Console.WriteLine((num == 0) ? "No installed NeoRune mod is affected by the game update." : $"{num} mod(s) use something the game update changed.");
		return (num != 0) ? 1 : 0;
	}

	private static ApiDump Discover()
	{
		using GameProcess process = GameProcess.Attach();
		return new ReflectionDumper(process, delegate
		{
		}).Dump();
	}

	private static string SaveDump(ApiDump dump)
	{
		Directory.CreateDirectory(CacheDirectory);
		string buildId = dump.Game.BuildId;
		string text = ((buildId != null && buildId.Length > 0) ? buildId : dump.Game.ExeStamp);
		string text2 = Path.Combine(CacheDirectory, text + ".json.gz");
		string tempFileName = Path.GetTempFileName();
		dump.Save(tempFileName);
		using (FileStream fileStream = File.OpenRead(tempFileName))
		{
			using GZipStream destination = new GZipStream(File.Create(text2), CompressionLevel.SmallestSize);
			fileStream.CopyTo(destination);
		}
		File.Delete(tempFileName);
		Console.WriteLine("Saved the API of " + GameInstall.Describe(dump.Game) + " to " + text2);
		return text2;
	}
}
