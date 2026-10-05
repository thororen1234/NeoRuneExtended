using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NeoRuneExtended.Assets;
using NeoRuneExtended.Bindings;
using NeoRuneExtended.Compiler;
using NeoRuneExtended.Discovery;
using NeoRuneExtended.Packaging;

namespace NeoRuneExtended.Cli;

internal static class Commands
{
	public const string Help = "NeoRuneExtended - C# mods for Minecraft Dungeons II\n\nUsage: neorunex <command> [options]   (arguments can also come from @file.rsp)\n\n  build      Compile a mod's C# sources to Blueprint packages and pack them.\n               --mod <Name> --out <dir> --source <file.cs>... --ref <file.dll>...\n               [--install] [--game <game folder>] [--tools <dir with retoc/repak>]\n               [--title <name>] [--version <x.y>] [--author <name>] [--author-url <url>]\n               [--description <text>]   (the Mods tab of Blueprint Loader 2.0)\n  install    Copy a built mod into the game's ~mods folder (skips unchanged files).\n               --mod <Name> --pak <dir with Name_P.*> [--game <game folder>]\n               [--bindings <NeoRune.Game.json>]  (warns NR0003 if the game is another build)\n  doctor     Check the setup: game folders, mod loader, installed mods, and the game build\n             against your NeoRuneExtended.Sdk versions. Run it in a mod folder to also check that mod.\n               [--game <game folder>] [--project <mod folder or .csproj>]\n  discover   Dump the running game's reflected API (read-only memory access).\n               [--out <api.json.gz>]\n  bindings   Generate the C# bindings project from an API dump.\n               --api <api.json[.gz]> --out <dir> --abstractions <NeoRuneExtended.Abstractions.csproj>\n  diff       Report API changes between two dumps (run after a game patch).\n               <old.json.gz> <new.json.gz>\n  log        Print what a mod wrote with NeoRune.Log.\n               <ModName> | <file.sav>   [--follow]  (keep printing new lines while you play)\n  disasm     Print the bytecode of a generated (or any versioned) Blueprint package.\n               <file.uasset>";

	private static string ToolsDir(Args a)
	{
		return a.Get("tools") ?? new string[2]
		{
			Path.Combine(AppContext.BaseDirectory, "tools"),
			AppContext.BaseDirectory
		}.FirstOrDefault((string d) => File.Exists(Path.Combine(d, "retoc.exe"))) ?? throw new UsageException("retoc/repak not found, pass --tools");
	}

	public static int Build(Args a)
	{
		string text = a.Require("mod");
		string path = a.Require("out");
		IReadOnlyList<string> readOnlyList = a.All("source");
		if (readOnlyList.Count == 0)
		{
			throw new UsageException("no --source files");
		}
		ModCompiler modCompiler = new ModCompiler(text, readOnlyList, a.All("ref"), a.All("define"));
		// What Blueprint Loader 2.0 shows in the Mods tab. "\n" in the description is a line break.
		string text2 = a.Get("title");
		modCompiler.Info = new ModInfoData
		{
			ModName = (string.IsNullOrEmpty(text2) ? text : text2),
			Version = a.Get("version"),
			Author = a.Get("author"),
			AuthorUrl = a.Get("author-url"),
			Description = a.Get("description")?.Replace("\\n", "\n")
		};
		string text3 = Path.Combine(path, "Assets");
		bool flag = modCompiler.Compile(text3);
		foreach (ModDiagnostic diagnostic in modCompiler.Diagnostics)
		{
			Console.WriteLine(diagnostic);
		}
		if (!flag)
		{
			return 1;
		}
		string text4 = new ModPackager(ToolsDir(a)).Pack(text, text3, Path.Combine(path, "Pak"), Path.Combine(path, "Temp"));
		Console.WriteLine("NeoRuneExtended: " + text + " -> " + Path.GetDirectoryName(text4));
		if (a.Flag("install"))
		{
			string gameDirectory = ModPackager.ResolveGame(a.Get("game"));
			if (GameInstall.IsRunning())
			{
				Console.WriteLine("warning NR0002: " + text + " was built but not installed: close Minecraft Dungeons II first (it locks ~mods).");
				return 0;
			}
			Console.WriteLine("NeoRuneExtended: installed to " + ModPackager.Install(text4, text, gameDirectory));
		}
		return 0;
	}

	public static int Install(Args a)
	{
		string mod = a.Require("mod");
		string path = a.Require("pak");
		string stem = Path.Combine(path, mod + "_P");
		if (!File.Exists(stem + ".utoc"))
		{
			throw new InvalidOperationException(stem + ".utoc not found; build the mod first");
		}
		string text = ModPackager.ResolveGame(a.Get("game"));
		string text2 = a.Get("bindings");
		if (text2 != null && text2.Length > 0)
		{
			WarnIfOtherBuild(mod, text, text2);
		}
		string target = Path.Combine(GameInstall.ModsDirectory(text), mod);
		if (new string[3] { ".pak", ".utoc", ".ucas" }.All(delegate(string ext)
		{
			string path2 = Path.Combine(target, mod + "_P" + ext);
			return File.Exists(path2) && ((ReadOnlySpan<byte>)File.ReadAllBytes(path2).AsSpan()).SequenceEqual((ReadOnlySpan<byte>)File.ReadAllBytes(stem + ext));
		}))
		{
			Console.WriteLine("NeoRuneExtended: " + mod + " is already installed and up to date");
			return 0;
		}
		if (GameInstall.IsRunning())
		{
			Console.WriteLine("warning NR0002: " + mod + " was not installed: close Minecraft Dungeons II (it locks ~mods) and build again.");
			return 0;
		}
		Console.WriteLine("NeoRuneExtended: installed " + mod + " to " + ModPackager.Install(stem, mod, text));
		return 0;
	}

	private static void WarnIfOtherBuild(string mod, string game, string bindingsFile)
	{
		GameBuild gameBuild = BindingsProject.ReadGameBuild(bindingsFile);
		if (gameBuild != null)
		{
			GameBuild b = GameInstall.Identify(game);
			if (GameInstall.SameBuild(gameBuild, b) == false)
			{
				Console.WriteLine($"warning NR0003: {mod}: Minecraft Dungeons II is {GameInstall.Describe(b)}, but this NeoRuneExtended.Sdk was made for {GameInstall.Describe(gameBuild)}. The mod may fail to load or misbehave if the game changed something it uses: " + "update the version in <Project Sdk=\"NeoRune.Sdk/x.y.z\"> when a newer SDK is out, or set NeoRuneCheckGameBuild=false to silence this.");
			}
		}
	}

	public static int Discover(Args a)
	{
		using GameProcess process = GameProcess.Attach();
		ApiDump apiDump = new ReflectionDumper(process, Console.WriteLine).Dump();
		string text = a.Get("out");
		if (text == null)
		{
			string buildId = apiDump.Game.BuildId;
			text = "api-" + ((buildId != null && buildId.Length > 0) ? buildId : apiDump.Game.ExeStamp) + ".json.gz";
		}
		string text2 = text;
		if (text2.EndsWith(".gz"))
		{
			string tempFileName = Path.GetTempFileName();
			apiDump.Save(tempFileName);
			using (FileStream fileStream = File.OpenRead(tempFileName))
			{
				using GZipStream destination = new GZipStream(File.Create(text2), CompressionLevel.SmallestSize);
				fileStream.CopyTo(destination);
			}
			File.Delete(tempFileName);
		}
		else
		{
			apiDump.Save(text2);
		}
		Console.WriteLine($"Wrote {text2}: {apiDump.Types.Count} types (build {apiDump.Game.BuildId}, exe {apiDump.Game.ExeStamp})");
		return 0;
	}

	public static int Bindings(Args a)
	{
		ApiDump apiDump = BindingsProject.LoadDump(a.Require("api"));
		string text = a.Require("out");
		Directory.CreateDirectory(text);
		List<string> list = BindingsProject.Write(apiDump, text, Path.GetFullPath(a.Require("abstractions")));
		File.WriteAllLines(Path.Combine(text, "skipped.txt"), list);
		Console.WriteLine($"Generated bindings for {apiDump.Types.Count} types into {text} ({list.Count} members skipped, see skipped.txt)");
		return 0;
	}

	public static int Diff(Args a)
	{
		if (a.Positional.Count != 2)
		{
			throw new UsageException("diff needs <old> <new>");
		}
		List<string> list = ApiDiff.Compare(BindingsProject.LoadDump(a.Positional[0]), BindingsProject.LoadDump(a.Positional[1]));
		foreach (string item in list)
		{
			Console.WriteLine(item);
		}
		Console.WriteLine((list.Count == 0) ? "No breaking API changes." : $"{list.Count} change(s).");
		return 0;
	}

	public static int Disasm(Args a)
	{
		if (a.Positional.Count != 1)
		{
			throw new UsageException("disasm needs <file.uasset>");
		}
		Console.Write(Disassembler.Disassemble(a.Positional[0]));
		return 0;
	}
}
