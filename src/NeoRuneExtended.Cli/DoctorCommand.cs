using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NeoRuneExtended.Bindings;
using NeoRuneExtended.Discovery;

namespace NeoRuneExtended.Cli;

internal static class DoctorCommand
{
	private sealed record Project(string Path, string Name, string? SdkVersion, string? GameDir, string Directory);

	private static int problems;

	private static void Ok(string text)
	{
		Console.WriteLine("  [ok]   " + text);
	}

	private static void Info(string text)
	{
		Console.WriteLine("         " + text);
	}

	private static void Warn(string text)
	{
		Console.WriteLine("  [warn] " + text);
	}

	private static void Fail(string text)
	{
		problems++;
		Console.WriteLine("  [FAIL] " + text);
	}

	public static int Run(Args a)
	{
		problems = 0;
		Project project = FindProject(a.Get("project"));
		Console.WriteLine("Game");
		string text = CheckGame(a.Get("game") ?? project?.GameDir);
		GameBuild build = ((text != null) ? GameInstall.Identify(text) : null);
		if (GameInstall.IsRunning())
		{
			Warn("The game is running: builds can't install mods until you close it (warning NR0002).");
		}
		else
		{
			Ok("The game is not running (builds can install).");
		}
		if (text != null && GameInstall.IsGdk(text))
		{
			Info("This is the Xbox app version (WinGDK): saves, and so mod logs, go to Xbox save storage instead of SaveGames.");
			if (LogCommand.XboxSaveFolders().Any())
			{
				Ok("Xbox save storage found: \"neorunex log <Mod>\" reads logs from there.");
			}
			else
			{
				Warn("No Xbox save storage found yet (it appears after the game first saves). Until then \"neorunex log\" has nothing to read.");
			}
		}
		if (text != null)
		{
			Console.WriteLine();
			Console.WriteLine("Mods folder");
			CheckMods(text);
		}
		Console.WriteLine();
		Console.WriteLine("NeoRuneExtended.Sdk versions (NuGet cache)");
		CheckSdks(build);
		if (project != null)
		{
			Console.WriteLine();
			Console.WriteLine("Mod project " + project.Path);
			CheckProject(project, text, build);
		}
		Console.WriteLine();
		Console.WriteLine((problems > 0) ? $"{problems} problem(s) found: fix the [FAIL] lines above (more help in docs\\troubleshooting.md of the NeoRuneExtended download)." : "No problems found. If a mod still seems not to run: mods that only log show nothing in game; check with \"neorunex log <ModName>\" after reaching the main menu.");
		return (problems != 0) ? 1 : 0;
	}

	private static string? CheckGame(string? configured)
	{
		if (configured != null && configured.Length > 0)
		{
			if (!GameInstall.IsGame(configured))
			{
				Fail("\"" + configured + "\" (NeoRuneGameDir / --game) is not a game folder: it must contain Dungeons\\Content\\Paks.");
				return null;
			}
			Ok("Game folder (configured): " + configured);
			Info("Build: " + GameInstall.Describe(GameInstall.Identify(configured)));
			return configured;
		}
		List<string> list = GameInstall.FindAll();
		if (list.Count == 0)
		{
			Fail("Minecraft Dungeons II was not found. Looked in:");
			foreach (string item in GameInstall.SearchedLocations())
			{
				Info(item);
			}
			Info("Set NeoRuneGameDir in your mod's .csproj to the folder that contains \"Dungeons\", or pass --game here.");
			return null;
		}
		Ok("Game folder: " + list[0]);
		Info("Build: " + GameInstall.Describe(GameInstall.Identify(list[0])));
		foreach (string item2 in list.Skip(1))
		{
			Warn($"Another install: {item2} ({GameInstall.Describe(GameInstall.Identify(item2))}). Builds install into the first one; set NeoRuneGameDir to pick another.");
		}
		return list[0];
	}

	private static void CheckMods(string game)
	{
		string text = GameInstall.PaksDirectory(game);
		string text2 = GameInstall.ModsDirectory(game);
		if (!Directory.Exists(text2))
		{
			Fail(text2 + " does not exist. Install a mod loader (BetterBlueprintLoader or Blueprint Loader), which goes there.");
			return;
		}
		string text3 = Directory.EnumerateFiles(text, "*.pak", SearchOption.AllDirectories).FirstOrDefault((string f) => IsLoader(Path.GetFileName(f)));
		if (text3 == null)
		{
			Fail("No mod loader is installed. Mods need BetterBlueprintLoader or Blueprint Loader (https://www.nexusmods.com/minecraftdungeons2/mods/2) in ~mods.");
		}
		else
		{
			string text4 = text3;
			string text5 = text4.Substring(0, text4.Length - 4);
			if (!File.Exists(text5 + ".utoc") || !File.Exists(text5 + ".ucas"))
			{
				Fail($"The mod loader is incomplete: {Path.GetFileName(text5)}.pak needs its .utoc and .ucas next to it ({Path.GetDirectoryName(text3)}).");
			}
			else
			{
				Ok("Mod loader: " + Path.GetRelativePath(text, Path.GetDirectoryName(text3)));
			}
		}
		foreach (string item in Directory.EnumerateDirectories(text2).OrderBy<string, string>((string d) => d, StringComparer.OrdinalIgnoreCase))
		{
			string fileName = Path.GetFileName(item);
			if (IsLoader(fileName))
			{
				continue;
			}
			List<string> files = Directory.EnumerateFiles(item).Select(Path.GetFileName).ToList();
			List<string> list = (from f in files
				where f.EndsWith(".pak") || f.EndsWith(".utoc") || f.EndsWith(".ucas")
				select Path.GetFileNameWithoutExtension(f)).Distinct().ToList();
			if (list.Count == 0)
			{
				Warn(fileName + ": no .pak/.utoc/.ucas files");
				continue;
			}
			foreach (string s in list)
			{
				List<string> list2 = new string[3] { ".pak", ".utoc", ".ucas" }.Where((string ext) => !files.Contains(s + ext)).ToList();
				if (list2.Count > 0)
				{
					Fail($"{fileName}: {s} is missing {string.Join(", ", list2)} (a mod needs all three files).");
				}
				else
				{
					string text6 = LogCommand.Find(fileName);
					Ok((text6 != null) ? $"{fileName} (last logged {File.GetLastWriteTime(text6):yyyy-MM-dd HH:mm}: neorunex log {fileName})" : (fileName ?? ""));
				}
			}
		}
	}

	private static string NuGetPackages()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
		if (environmentVariable == null || environmentVariable.Length <= 0)
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
		}
		return environmentVariable;
	}

	private static void CheckSdks(GameBuild? build)
	{
		string path = Path.Combine(NuGetPackages(), "neoruneextended.sdk");
		if (!Directory.Exists(path))
		{
			Info("None yet (they are downloaded by the first \"dotnet build\" of a mod).");
			return;
		}
		foreach (string item in Directory.EnumerateDirectories(path).OrderBy<string, string>((string d) => d, StringComparer.OrdinalIgnoreCase))
		{
			string fileName = Path.GetFileName(item);
			GameBuild gameBuild = BindingsProject.ReadGameBuild(Path.Combine(item, "ref", "NeoRune.Game.json"));
			if (gameBuild == null)
			{
				Info(fileName + ": made before 0.2, game build not recorded");
				continue;
			}
			bool? flag = ((build == null) ? ((bool?)null) : GameInstall.SameBuild(gameBuild, build));
			string text = fileName + ": made for " + GameInstall.Describe(gameBuild);
			if (flag == false)
			{
				Warn(text + " (not your game's build)");
			}
			else if (flag == true)
			{
				Ok(text + " (matches your game)");
			}
			else
			{
				Info(text);
			}
		}
	}

	private static Project? FindProject(string? hint)
	{
		string path = hint ?? Environment.CurrentDirectory;
		if (Directory.Exists(path))
		{
			path = Directory.EnumerateFiles(path, "*.csproj").FirstOrDefault() ?? "";
		}
		if (!File.Exists(path))
		{
			return null;
		}
		string text = File.ReadAllText(path);
		Match match = Regex.Match(text, "NeoRuneExtended\\.Sdk/([^\"\\s]+)");
		if (!match.Success && !text.Contains("NeoRuneExtended.Sdk"))
		{
			return null;
		}
		return new Project(path, Property("NeoRuneModName") ?? Path.GetFileNameWithoutExtension(path), match.Success ? match.Groups[1].Value : null, Property("NeoRuneGameDir"), Path.GetDirectoryName(Path.GetFullPath(path)));
		string? Property(string name)
		{
			Match match2 = Regex.Match(text, $"<{name}>([^<]*)</{name}>");
			if (match2 == null || !match2.Success)
			{
				return null;
			}
			return match2.Groups[1].Value.Trim();
		}
	}

	private static void CheckProject(Project p, string? game, GameBuild? build)
	{
		if (p.SdkVersion == null)
		{
			Warn("Could not read the NeoRuneExtended.Sdk version from <Project Sdk=\"NeoRuneExtended.Sdk/x.y.z\">.");
		}
		else
		{
			string text = Path.Combine(NuGetPackages(), "neoruneextended.sdk", p.SdkVersion.ToLowerInvariant());
			if (!Directory.Exists(text))
			{
				Info("NeoRuneExtended.Sdk " + p.SdkVersion + " is not downloaded yet: \"dotnet build\" downloads it.");
			}
			else
			{
				Ok("NeoRuneExtended.Sdk " + p.SdkVersion);
				GameBuild gameBuild = BindingsProject.ReadGameBuild(Path.Combine(text, "ref", "NeoRune.Game.json"));
				if (gameBuild != null && build != null && GameInstall.SameBuild(gameBuild, build) == false)
				{
					Warn($"This SDK was made for {GameInstall.Describe(gameBuild)}, your game is {GameInstall.Describe(build)}. Use a newer SDK when one is out.");
				}
			}
		}
		string text2 = Path.Combine(p.Directory, "bin", "NeoRune", "Pak", p.Name + "_P.utoc");
		if (!File.Exists(text2))
		{
			Fail("Not built yet: run \"dotnet build\" in " + p.Directory + ".");
			return;
		}
		Ok("Built: " + Path.GetDirectoryName(text2));
		if (game != null)
		{
			string installed = Path.Combine(GameInstall.ModsDirectory(game), p.Name);
			string text3 = text2;
			string stem = text3.Substring(0, text3.Length - 5);
			List<string> list = new string[3] { ".pak", ".utoc", ".ucas" }.Where(delegate(string ext)
			{
				string path = Path.Combine(installed, p.Name + "_P" + ext);
				return !File.Exists(path) || !((ReadOnlySpan<byte>)File.ReadAllBytes(path).AsSpan()).SequenceEqual((ReadOnlySpan<byte>)File.ReadAllBytes(stem + ext));
			}).ToList();
			if (list.Count == 3)
			{
				Fail($"Not installed in {installed}. Close the game and run \"dotnet build\" again (or \"neorunex install --mod {p.Name} --pak {Path.GetDirectoryName(text2)}\").");
			}
			else if (list.Count > 0)
			{
				Fail("The installed copy is out of date (" + string.Join(", ", list) + " differ). Close the game and run \"dotnet build\" again.");
			}
			else
			{
				Ok("Installed and up to date: " + installed);
			}
		}
	}

	/// <summary>A mod loader's pak or folder: BetterBlueprintLoader or Blueprint Loader.</summary>
	private static bool IsLoader(string name)
	{
		return name.StartsWith("BlueprintLoader", StringComparison.OrdinalIgnoreCase) || name.StartsWith("BetterBlueprintLoader", StringComparison.OrdinalIgnoreCase);
	}
}
